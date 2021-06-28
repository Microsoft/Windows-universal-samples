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

using Microsoft.Samples.SimpleCommunication;
using SDKTemplate;
using System;
using System.Threading.Tasks;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace SimpleCommunication
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class LowLatency : Page
    {
        private enum State
        {
            Initializing,
            Waiting,
            Previewing,
            Streaming,
            End
        }

        private enum LatencyMode
        {
            NormalLatency,
            LowLatency
        }

        // A pointer back to the main page.  This is needed if you want to call methods in MainPage such
        // as NotifyUser()
        MainPage rootPage = MainPage.Current;

        CaptureDevice device = null;
        State? currentState = null;
        State? previousState = null;
        VideoEncodingProperties previewEncodingProperties = null;
        LatencyMode? mode = null;
        const LatencyMode defaultMode = LatencyMode.NormalLatency;

        public LowLatency()
        {
            this.InitializeComponent();

            rootPage.EnsureMediaExtensionManager();

            mode = LatencyMode.NormalLatency;
            LatencyModeToggle.IsOn = (mode == LatencyMode.LowLatency);
            rootPage.NotifyUser("Initializing...Please wait.", NotifyType.StatusMessage);
        }

        /// <summary>
        /// The media element visible in the local host client area
        /// </summary>
        private MediaElement LocalhostVideo
        {
            get
            {
                return (mode == LatencyMode.NormalLatency) ? PlaybackVideo : RealTimePlaybackVideo;
            }
        }

        /// <summary>
        /// The media element hidden in the local host client area
        /// </summary>
        private MediaElement HiddenVideo
        {
            get
            {
                return (mode == LatencyMode.NormalLatency) ? RealTimePlaybackVideo : PlaybackVideo;
            }
        }

        /// <summarycancel
        /// Initializes the scenario
        /// </summary>
        /// <returns></returns>
        private async Task InitializeAsync()
        {
            var streamFilteringCriteria = new
            {
                HorizontalResolution = (uint)480,
                SubType = "YUY2"
            };
            currentState = State.Initializing;
            device = new CaptureDevice();

            PreviewVideo.Visibility = Visibility.Collapsed;
            WebcamPreviewPoster.Visibility = Visibility.Visible;
            PreviewButton.Content = "Start Preview";
            LoopbackClientButton.IsEnabled = false;

            mode = defaultMode;
            LatencyModeToggle.IsOn = (mode == LatencyMode.LowLatency);
            LatencyModeToggle.IsEnabled = false;

            await device.InitializeAsync();
            var setting = await device.SelectPreferredCameraStreamSettingAsync(MediaStreamType.VideoPreview, ((x) =>
            {
                var previewStreamEncodingProperty = x as Windows.Media.MediaProperties.VideoEncodingProperties;

                return (previewStreamEncodingProperty.Width >= streamFilteringCriteria.HorizontalResolution &&
                    previewStreamEncodingProperty.Subtype == streamFilteringCriteria.SubType);
            }));

            previewEncodingProperties = setting as VideoEncodingProperties;

            PreviewSetupCompleted();
        }

        private void PreviewSetupCompleted()
        {
            device.CaptureFailed += Device_CaptureFailed;
            device.IncomingConnectionArrived += Device_IncomingConnectionArrived;
            previousState = currentState;
            PreviewButton.IsEnabled = true;
            rootPage.NotifyUser("Done. Tap or click 'Start Preview' button to start webcam preview", NotifyType.StatusMessage);

            currentState = State.Waiting;
        }


        void Device_IncomingConnectionArrived(object sender, IncomingConnectionEventArgs e)
        {
            e.Accept();
        }

        async void Device_CaptureFailed(object sender, MediaCaptureFailedEventArgs e)
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, (() =>
            {
                rootPage.NotifyUser(e.Message, NotifyType.ErrorMessage);
            }));
        }

        async void Window_VisibilityChanged(object sender, Windows.UI.Core.VisibilityChangedEventArgs e)
        {
            if (!e.Visible)
            {
                await CleanupAsync();
            }
            else
            {
                try
                {
                    await InitializeAsync();
                }
                catch (Exception err)
                {
                    rootPage.NotifyUser("Camera initialization error: " + err.Message + "\n Try restarting the sample.", NotifyType.ErrorMessage);
                }
            }
        }

        /// <summary>
        /// Invoked when this page is about to be displayed in a Frame.
        /// </summary>
        /// <param name="e">Event data that describes how this page was reached.  The Parameter
        /// property is typically used to configure the page.</param>
        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            var cameraEnumTask = CaptureDevice.CheckForRecordingDeviceAsync();

            var cameraFound = await cameraEnumTask;

            if (cameraFound)
            {
                try
                {
                    await InitializeAsync();

                    Window.Current.VisibilityChanged += Window_VisibilityChanged;
                }
                catch (Exception err)
                {
                    rootPage.NotifyUser("Camera initialization error: " + err.Message + "\n Try restarting the sample.", NotifyType.ErrorMessage);
                }
            }
            else
            {
                rootPage.NotifyUser("A machine with a camera and a microphone is required to run this sample.", NotifyType.ErrorMessage);
            }
        }

        protected async override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            Window.Current.VisibilityChanged -= Window_VisibilityChanged;
            await CleanupAsync();

            base.OnNavigatingFrom(e);
        }

        /// <summary>
        /// This is the click handler for the 'Preview' button.  It starts or stops 
        /// camera preview and streaming
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void PreviewButton_Click(object sender, RoutedEventArgs e)
        {
            Button b = sender as Button;
            if (b != null && device != null)
            {
                previousState = currentState;
                try
                {
                    switch (currentState)
                    {
                        case State.Waiting:
                            await Waiting_PreviewButtonClicked();
                            break;
                        case State.Previewing:
                            await Previewing_PreviewButtonClicked();
                            break;
                        case State.Streaming:
                            await Streaming_PreviewButtonClicked();
                            break;
                        default:
                            break;
                    }
                }
                catch (Exception exc)
                {
                    rootPage.NotifyUser(exc.Message, NotifyType.ErrorMessage);
                }
            }
        }

        private async Task Previewing_PreviewButtonClicked()
        {
            Task stopPreviewTask = device.CaptureSource.StopPreviewAsync().AsTask();

            PreviewVideo.Visibility = Visibility.Collapsed;
            WebcamPreviewPoster.Visibility = Visibility.Visible;
            PreviewButton.Content = "Start Preview";
            LoopbackClientButton.IsEnabled = false;

            await stopPreviewTask;
            currentState = State.Waiting;
        }

        private async Task Waiting_PreviewButtonClicked()
        {
            rootPage.NotifyUser("", NotifyType.StatusMessage);
            PreviewVideo.Source = device.CaptureSource;
            await device.CaptureSource.StartPreviewAsync();

            WebcamPreviewPoster.Visibility = Visibility.Collapsed;
            PreviewVideo.Visibility = Visibility.Visible;
            PreviewButton.Content = "Stop Preview";
            LoopbackClientButton.IsEnabled = true;

            currentState = State.Previewing;
        }

        private async Task Streaming_PreviewButtonClicked()
        {
            LocalhostVideo.Stop();
            await device.StopRecordingAsync();
            LocalhostVideo.Source = null;
            await Previewing_PreviewButtonClicked();

            mode = defaultMode;
            LatencyModeToggle.IsOn = (mode == LatencyMode.LowLatency);
            LatencyModeToggle.IsEnabled = false;

            LocalhostVideo.Visibility = Windows.UI.Xaml.Visibility.Visible;
            HiddenVideo.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
        }

        /// <summary>
        /// This is the click handler for the 'LoopbackClient' button.  It displays captured video
        /// recieved over the localhost network interface using a custom network media sink and media
        /// sources via a custom scheme
        /// </summary>
        /// <param name="sender">The LoopbackClient button</param>
        /// <param name="e">The button click routed event arguments</param>
        private async void LoopbackClientButton_Click(object sender, RoutedEventArgs e)
        {
            Button b = sender as Button;
            if (b != null)
            {
                previousState = currentState;
                await StartRecordingToCustomSink();

                if (currentState == State.Previewing)
                {
                    LocalhostVideo.Source = new Uri("stsp://localhost");
                    LatencyModeToggle.IsEnabled = true;
                    LoopbackClientButton.IsEnabled = false;
                    HiddenVideo.Visibility = Windows.UI.Xaml.Visibility.Collapsed;

                    currentState = State.Streaming;
                }
            }
        }

        private async Task StartRecordingToCustomSink()
        {
            // Use the MP4 preset to an obtain H.264 video encoding profile
            var mep = MediaEncodingProfile.CreateMp4(VideoEncodingQuality.Vga);
            mep.Audio = null;
            mep.Container = null;
            if (previewEncodingProperties != null)
            {
                mep.Video.Width = previewEncodingProperties.Width;
                mep.Video.Height = previewEncodingProperties.Height;
            }

            await device.StartRecordingAsync(mep);
        }

        private async void LatencyModeToggle_Toggled(object sender, RoutedEventArgs e)
        {
            if (currentState == State.Streaming)
            {
                try
                {
                    LocalhostVideo.Stop();

                    await device.StopRecordingAsync();
                    mode = (mode == LatencyMode.NormalLatency) ? LatencyMode.LowLatency : LatencyMode.NormalLatency;
                    await StartRecordingToCustomSink();

                    HiddenVideo.Source = null;
                    LocalhostVideo.Source = new Uri("stsp://localhost");

                    LocalhostVideo.Visibility = Windows.UI.Xaml.Visibility.Visible;
                    HiddenVideo.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                }
                catch (Exception exc)
                {
                    rootPage.NotifyUser(exc.Message, NotifyType.ErrorMessage);
                }
            }
        }

        private async Task CleanupAsync()
        {
            try
            {
                if (currentState == State.Previewing || currentState == State.Streaming)
                {
                    await device.CaptureSource.StopPreviewAsync();

                    if (currentState == State.Streaming)
                    {
                        LocalhostVideo.Stop();
                    }
                }


                await device.CleanUpAsync();
            }
            catch (Exception)
            {
            }
            finally
            {
                LocalhostVideo.Source = null;
            }
        }
    }
}
