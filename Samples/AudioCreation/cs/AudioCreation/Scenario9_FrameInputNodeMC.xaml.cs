//*********************************************************
//
// Copyright (c) Microsoft. All rights reserved.
// This code is licensed under the MIT License (MIT).
// THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
// ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
// IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
// PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.
//
//*********************************************************

using SDKTemplate;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Windows.Foundation;
using System.Linq;
using Windows.Media;
using Windows.Media.Audio;
using Windows.Media.MediaProperties;
using Windows.Media.Render;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace AudioCreation
{
    // We are initializing a COM interface for use within the namespace
    // This interface allows access to memory at the byte level which we need to populate audio data that is generated
    // we did this in this namespace in scenario3 so we can just reuse it.
//    [ComImport]
//    [Guid("5B0D3235-4DBA-4D44-865E-8F1D0E4FD04D")]
//    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]

//    unsafe interface IMemoryBufferByteAccess
//    {
//        void GetBuffer(out byte* buffer, out uint capacity);
//    }

    public class NoteMC
    {
        public AudioGraph parentGraph;
        public AudioFrameInputNode noteSynth;
        double angle;
        double previousSample;  // for wrapping from buffer to buffer

        private List<double> frequencyList = new List<double>();
        private List<double> angleList = new List<double>();

        public NoteMC( AudioGraph graph, AudioFrameInputNode node, double freq)
        {
            parentGraph = graph;
            noteSynth = node;
            AddFrequency(freq);
            AddAngle(0.0);

            previousSample = 0.0;  // always start notes on the baseline
        }

        public bool AddFrequency( double freq )
        {
            if ( frequencyList.Count > parentGraph.EncodingProperties.ChannelCount )
            {
                return false;
            }

            frequencyList.Add(freq);

            return true;
        }

        public int GetFrequencyCount()
        {
            return frequencyList.Count;
        }

        public bool AddAngle(double angle)
        {
            if (angleList.Count > parentGraph.EncodingProperties.ChannelCount)
            {
                return false;
            }

            angleList.Add(angle);

            return true;
        }

        public bool UpdateAngle(double angle, int index )
        {
            if (index >= parentGraph.EncodingProperties.ChannelCount)
            {
                return false;
            }

            angleList[index] = angle;
            return true;
        }

        unsafe internal AudioFrame GenerateAudioData(uint samples)
        {
            // Buffer size is (number of samples) * (size of each sample)
            // We choose to generate single channel (mono) audio. For multi-channel, multiply by number of channels
            uint bufferSize = samples * parentGraph.EncodingProperties.ChannelCount * sizeof(float);
            AudioFrame frame = new Windows.Media.AudioFrame(bufferSize);

            using (AudioBuffer buffer = frame.LockBuffer(AudioBufferAccessMode.Write))
            using (IMemoryBufferReference reference = buffer.CreateReference())
            {
                byte* dataInBytes;
                uint capacityInBytes;
                float* dataInFloat;

                // Get the buffer from the AudioFrame
                ((IMemoryBufferByteAccess)reference).GetBuffer(out dataInBytes, out capacityInBytes);

                // Cast to float since the data we are generating is float
                dataInFloat = (float*)dataInBytes;

                float amplitude = 0.3f;
                int sampleRate = (int)parentGraph.EncodingProperties.SampleRate;

                int bufPointer = 0;
                // Generate a sine wave and populate the values in the memory buffer
                for (int i = 0; i < samples; i++)
                {
                    // for multiple channels, samples are interleaved
                    for (int ch = 0; ch < parentGraph.EncodingProperties.ChannelCount; ch++)
                    {
                        double sampleIncrement = (frequencyList[ch] * (Math.PI * 2)) / sampleRate;

                        double sinValue = amplitude * Math.Sin(angleList[ch]);
                        dataInFloat[bufPointer++] = (float)sinValue;
                        angleList[ch] += sampleIncrement;
                    }
                }
            }

            return frame;
        }

    };

    /// <summary>
    /// Scenario 9: synth using mixing of known frequencies for capacity tests, but multiple channels to save nodes
    /// </summary>
    public sealed partial class Scenario9_FrameInputNodeMC : Page
    {
        public string setupDescription;
        private MainPage rootPage;
        private AudioGraph graph;
        private AudioDeviceOutputNode deviceOutputNode;
        //        private AudioFrameInputNode frameInputNode;

        static double octaveScale = 16.0;
        private readonly double startC = (261.625565300599F / octaveScale);  // down n octaves from midC
        private readonly double startE = (329.62755691287F / octaveScale);
        private readonly double startG = (391.995435981749F / octaveScale);

        private int noteCount = 16;
        private int bufferLength = 480;
        private int sampleCount;

        private Dictionary<AudioFrameInputNode, NoteMC> inputNotes = new Dictionary<AudioFrameInputNode, NoteMC>();
        private List<double> allFrequencyList = new List<double>();

        public Scenario9_FrameInputNodeMC()
        {
            this.InitializeComponent();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            rootPage = MainPage.Current;
            await CreateAudioGraph();
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            if (graph != null)
            {
                graph.Dispose();
            }
        }

        private void StartNotes()
        {
            foreach( var kv in inputNotes)
            {
                var n = kv.Key;
                n.Start();
            }
        }

        private void StopNotes()
        {
            foreach (var kv in inputNotes)
            {
                var n = kv.Key;
                n.Stop();
            }
        }

        private void CreateNotes(int noteCount, AudioEncodingProperties props,
                                 AudioDeviceOutputNode outputNode)
        {
            NoteMC currentNote = null;

            for (int i = 0; i < noteCount; i++)
            {
                double fr = 0;
                if (i < 3)
                {
                    switch (i)
                    {
                        case 0:
                            fr = startC;
                            break;
                        case 1:
                            fr = startE;
                            break;
                        case 2:
                            fr = startG;
                            break;
                    }
                }
                else
                {
                    var lastOfNote = allFrequencyList[i - 3];
                    fr = lastOfNote * 2.0;
                }

                allFrequencyList.Add(fr);

                // Initialize the Frame Input Node in the stopped state

                var nyQuist = graph.EncodingProperties.SampleRate / 2.0;
                // Hook up an event handler so we can start generating samples when needed
                // This event is triggered when the node is required to provide data

                if (fr > nyQuist) break;  // no need to generate notes higher than 
                // the nyQuist frequency which will just sound like noise.

                if (currentNote == null ||
                   (currentNote.GetFrequencyCount() >= graph.EncodingProperties.ChannelCount))
                {
                    var inNode = graph.CreateFrameInputNode(props);
                    inNode.AddOutgoingConnection(outputNode);

                    inNode.Stop();
                    inNode.QuantumStarted += node_QuantumStarted;

                    currentNote = new NoteMC(graph, inNode, fr);

                    inputNotes.Add(inNode, currentNote);
                }
                else
                {
                    currentNote.AddFrequency(fr);
                    currentNote.AddAngle(0.0);
                }
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (generateButton.Content.Equals("Generate Audio"))
            {
                StartNotes();

                generateButton.Content = "Stop";
                audioPipe.Fill = new SolidColorBrush(Colors.Blue);
            }
            else if (generateButton.Content.Equals("Stop"))
            {
                StopNotes();

                generateButton.Content = "Generate Audio";
                audioPipe.Fill = new SolidColorBrush(Color.FromArgb(255, 49, 49, 49));
            }
        }

        private async Task CreateAudioGraph()
        {
            // Create an AudioGraph with default settings
            AudioGraphSettings settings = new AudioGraphSettings(AudioRenderCategory.Media);
            settings.QuantumSizeSelectionMode = QuantumSizeSelectionMode.ClosestToDesired;
            settings.DesiredSamplesPerQuantum = bufferLength;
            sampleCount = settings.DesiredSamplesPerQuantum;

            var encoding = new AudioEncodingProperties();
            encoding.BitsPerSample = 32;
            encoding.Bitrate = 3072000;
            encoding.ChannelCount = 1;
            encoding.SampleRate = 48000;
            encoding.Subtype = "Float";
            // encoding.Type = "Audio"; // evidently set by the creation of the audiograph

            settings.EncodingProperties = encoding;

            CreateAudioGraphResult result = await AudioGraph.CreateAsync(settings);

            if (result.Status != AudioGraphCreationStatus.Success)
            {
                // Cannot create graph
                rootPage.NotifyUser(String.Format("AudioGraph Creation Error because {0}", result.Status.ToString()), NotifyType.ErrorMessage);
                return;
            }

            graph = result.Graph;

            Debug.WriteLine($"Set samples per quantum to {graph.SamplesPerQuantum}");

            // Create a device output node
            CreateAudioDeviceOutputNodeResult deviceOutputNodeResult = await graph.CreateDeviceOutputNodeAsync();
            if (deviceOutputNodeResult.Status != AudioDeviceNodeCreationStatus.Success)
            {
                // Cannot create device output node
                rootPage.NotifyUser(String.Format("Audio Device Output unavailable because {0}", deviceOutputNodeResult.Status.ToString()), NotifyType.ErrorMessage);
                speakerContainer.Background = new SolidColorBrush(Colors.Red);
            }

            deviceOutputNode = deviceOutputNodeResult.DeviceOutputNode;
            rootPage.NotifyUser("Device Output Node successfully created", NotifyType.StatusMessage);
            speakerContainer.Background = new SolidColorBrush(Colors.Green);

            // Create the FrameInputNode at the same format as the graph, except explicitly set mono.
            AudioEncodingProperties nodeEncodingProperties = graph.EncodingProperties;
//            nodeEncodingProperties.ChannelCount = 1;
            CreateNotes(noteCount, nodeEncodingProperties, deviceOutputNode);

            double lowNote = allFrequencyList[0];
            double hiNote = allFrequencyList[allFrequencyList.Count - 1];

            noteCount = inputNotes.Keys.Count;
            var mSLength = 1000.0 * (double)bufferLength / 48000.0;
            setupDescription = $"playing {noteCount} notes in {allFrequencyList.Count / 3} octaves ({lowNote:0.0} -> {hiNote:0.0}), {graph.SamplesPerQuantum} samples, in {inputNotes.Count}, {mSLength:0.0}mS buffers";
            DetailText.Text = setupDescription;

            frameContainer.Background = new SolidColorBrush(Colors.Green);

            // Start the graph since we will only start/stop the frame input node
            graph.Start();
        }

        private void node_QuantumStarted(AudioFrameInputNode sender, FrameInputNodeQuantumStartedEventArgs args)
        {
            // GenerateAudioData can provide PCM audio data by directly synthesizing it or reading from a file.
            // Need to know how many samples are required. In this case, the node is running at the same rate as the rest of the graph
            // For minimum latency, only provide the required amount of samples. Extra samples will introduce additional latency.
            uint numSamplesNeeded = (uint) args.RequiredSamples;

            if(numSamplesNeeded != 0)
            {
                var theNote = inputNotes[sender];
                AudioFrame audioData = theNote.GenerateAudioData(numSamplesNeeded);
                theNote.noteSynth.AddFrame(audioData);
            }
        }
    }
}
