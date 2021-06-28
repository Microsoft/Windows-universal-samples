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
using System.Text;
using Windows.ApplicationModel.Core;
using Windows.Networking;
using Windows.Networking.Connectivity;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace DatagramSocketSample
{
    class bufferData
    {
        public int stringLenth;
        public int count;
        public int tripCounter;
        public string messageString;
    }

    /// <summary>
    /// A page for fifth scenario.
    /// </summary>
    public sealed partial class Scenario5 : Page
    {
        // A pointer back to the main page.  This is needed if you want to call methods in MainPage such
        // as NotifyUser()
        private MainPage rootPage = MainPage.Current;
        
        private DatagramSocket listenerSocket = null;

        private Stopwatch latencySW = new Stopwatch();
        private uint tripCounter = 0;
        private uint byteCounter = 0;

        private List<bufferData> _bufferData = new List<bufferData>();

        public Scenario5()
        {
            this.InitializeComponent();
        }

        private void CloseListenerSocket()
        {
            if (listenerSocket != null)
            {
                // DatagramSocket.Close() is exposed through the Dispose() method in C#.
                // The call below explicitly closes the socket, freeing the UDP port that it is currently bound to.
                listenerSocket.Dispose();
                listenerSocket = null;
            }
        }

        /// <summary>
        /// Invoked when this page is about to be displayed in a Frame.
        /// </summary>
        /// <param name="e">Event data that describes how this page was reached.  The Parameter
        /// property is typically used to configure the page.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            MulticastRadioButton.IsChecked = true;
        }

        /// <summary>
        /// Invoked immediately before the Page is unloaded and is no longer the current source of a parent Frame.
        /// </summary>
        /// <param name="e">Event data representative of the navigation that will unload the current Page.</param>
        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            CloseListenerSocket();
        }

        // Sets up the UI to display the multicast scenario options.
        private void SetupMulticastScenarioUI()
        {
            RemoteAddressLabel.Text = "Multicast Group:";
            StartListener.Content = "Start listener and join multicast group";
            //            RemoteAddress.Text = "224.3.0.5";
            RemoteAddress.Text = "239.86.100.120";
            RemoteAddress.IsEnabled = false;
            SendMessageButton.IsEnabled = false;
            CloseListenerButton.IsEnabled = false;
            SendOutput.Text = "";
        }

        // Sets up the UI to display the broadcast scenario options.
        private void SetupBroadcastScenarioUI()
        {
            RemoteAddressLabel.Text = "Broadcast Address:";
            StartListener.Content = "Start listener";
            RemoteAddress.Text = "255.255.255.255";
            RemoteAddress.IsEnabled = false;
            SendMessageButton.IsEnabled = false;
            CloseListenerButton.IsEnabled = false;
            SendOutput.Text = "";
        }

        private void MulticastRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            CloseListenerSocket();
            SetupMulticastScenarioUI();
        }

        private void MulticastRadioButton_Unchecked(object sender, RoutedEventArgs e)
        {
            CloseListenerSocket();
            SetupBroadcastScenarioUI();
        }

        /// <summary>
        /// This is the click handler for the 'StartListener' button.
        /// </summary>
        /// <param name="sender">Object for which the event was generated.</param>
        /// <param name="e">Event's parameters.</param>
        private async void StartListener_Click(object sender, RoutedEventArgs e)
        {
            if (String.IsNullOrEmpty(ServiceName.Text))
            {
                rootPage.NotifyUser("Please provide a service name.", NotifyType.ErrorMessage);
                return;
            }

            if (listenerSocket != null)
            {
                rootPage.NotifyUser("A listener socket is already set up.", NotifyType.ErrorMessage);
                return;
            }

            bool isMulticastSocket = (MulticastRadioButton.IsChecked == true);
            listenerSocket = new DatagramSocket();
            listenerSocket.MessageReceived += MessageReceived;

            if (isMulticastSocket)
            {
                // DatagramSockets conduct exclusive (SO_EXCLUSIVEADDRUSE) binds by default, effectively blocking
                // any other UDP socket on the system from binding to the same local port. This is done to prevent
                // other applications from eavesdropping or hijacking a DatagramSocket's unicast traffic.
                //
                // Setting the MulticastOnly control option to 'true' enables a DatagramSocket instance to share its
                // local port with any Win32 sockets that are bound using SO_REUSEADDR/SO_REUSE_MULTICASTPORT and
                // with any other DatagramSocket instances that have MulticastOnly set to true. However, note that any
                // attempt to use a multicast-only DatagramSocket instance to send or receive unicast data will result
                // in an exception being thrown.
                //
                // This control option is particularly useful when implementing a well-known multicast-based protocol,
                // such as mDNS and UPnP, since it enables a DatagramSocket instance to coexist with other applications
                // running on the system that also implement that protocol.
                listenerSocket.Control.MulticastOnly = true;
            }

            // Start listen operation.
            try
            {
                await listenerSocket.BindServiceNameAsync(ServiceName.Text);

                if (isMulticastSocket)
                {
                    // Join the multicast group to start receiving datagrams being sent to that group.
                    listenerSocket.JoinMulticastGroup(new HostName(RemoteAddress.Text));

                    rootPage.NotifyUser(
                        "Listening on port " + listenerSocket.Information.LocalPort + " and joined to multicast group",
                        NotifyType.StatusMessage);
                }
                else
                {
                    rootPage.NotifyUser(
                        "Listening on port " + listenerSocket.Information.LocalPort,
                        NotifyType.StatusMessage);
                }

                // Enable scenario steps that require us to have an active listening socket.
                SendMessageButton.IsEnabled = true;
                CloseListenerButton.IsEnabled = true;
            }
            catch (Exception exception)
            {
                listenerSocket.Dispose();
                listenerSocket = null;

                // If this is an unknown status it means that the error is fatal and retry will likely fail.
                if (SocketError.GetStatus(exception.HResult) == SocketErrorStatus.Unknown)
                {
                    throw;
                }

                rootPage.NotifyUser(
                    "Start listening failed with error: " + exception.Message,
                    NotifyType.ErrorMessage);
            }
        }

        /// <summary>
        /// This is the click handler for the 'SendMessage' button.
        /// </summary>
        /// <param name="sender">Object for which the event was generated.</param>
        /// <param name="e">Event's parameters.</param>
        private async void SendMessage_Click(object sender, RoutedEventArgs e)
        {
            SendOutput.Text = "";

            try
            {
                IOutputStream outputStream;
                HostName remoteHostname = new HostName(RemoteAddress.Text);

                // GetOutputStreamAsync can be called multiple times on a single DatagramSocket instance to obtain
                // IOutputStreams pointing to various different remote endpoints. The remote hostname given to
                // GetOutputStreamAsync can be a unicast, multicast or broadcast address.
                outputStream = await listenerSocket.GetOutputStreamAsync(remoteHostname, ServiceName.Text);

                // Send out some multicast or broadcast data. Datagrams generated by the IOutputStream will use
                // <source host, source port> information obtained from the parent socket (i.e., 'listenSocket' in
                // this case).
                const string stringToSend = "Hello";
                DataWriter writer = new DataWriter(outputStream);
                byte[] str = Encoding.ASCII.GetBytes(stringToSend);
                var len = str.Length;

                _bufferData.Clear();

                latencySW.Restart();

                tripCounter = 0;
                byteCounter = 0;

                writer.WriteInt32(len);
                writer.WriteInt32(0x8000080);
                writer.WriteBytes(str);

                await writer.StoreAsync();

                SendOutput.Text = "\"" + stringToSend + "\" sent successfully.";
            }
            catch (Exception exception)
            {
                // If this is an unknown status it means that the error is fatal and retry will likely fail.
                if (SocketError.GetStatus(exception.HResult) == SocketErrorStatus.Unknown)
                {
                    throw;
                }

                rootPage.NotifyUser("Send failed with error: " + exception.Message, NotifyType.ErrorMessage);
            }
        }

        /// <summary>
        /// This is the click handler for the 'CloseListener' button.
        /// </summary>
        /// <param name="sender">Object for which the event was generated.</param>
        /// <param name="e">Event's parameters.</param>
        private void CloseListener_Click(object sender, RoutedEventArgs e)
        {
            CloseListenerSocket();

            // Disable scenario steps that require us to have an active listening socket.
            SendMessageButton.IsEnabled = false;
            CloseListenerButton.IsEnabled = false;
            SendOutput.Text = "";

            rootPage.NotifyUser("Listener closed", NotifyType.StatusMessage);
        }

        /// <summary>
        /// Message received handler
        /// </summary>
        /// <param name="socket">The socket object</param>
        /// <param name="eventArguments">The datagram event information</param>
        void MessageReceived(DatagramSocket socket, DatagramSocketMessageReceivedEventArgs eventArguments)
        {
            try
            {
                tripCounter++;
                // Interpret the incoming datagram's entire contents as a string.
                var stringLength = eventArguments.GetDataReader().ReadInt32();
                byteCounter += (uint)stringLength;

                byte[] bytes = new byte[stringLength];

                var count = eventArguments.GetDataReader().ReadInt32();
                eventArguments.GetDataReader().ReadBytes(bytes);

                string ut8String = Encoding.ASCII.GetString(bytes);

                _bufferData.Add(new bufferData()
                {
                    count = count,
                    stringLenth = stringLength,
                    messageString = ut8String,
                    tripCounter = (int)tripCounter
                });

                latencySW.Stop();
                var ticks = latencySW.ElapsedTicks;
                var uS = (double)ticks / (double)Stopwatch.Frequency * 1000000.0D;

                NotifyUserFromAsyncThread(
                    $" {tripCounter} trips, {byteCounter} bytes in {uS}uS Received data from remote peer (Remote Address: " +
                    eventArguments.RemoteAddress.CanonicalName +
                    ", Remote Port: " +
                    eventArguments.RemotePort + "): \"" +
                     ut8String + "\"",
                    NotifyType.StatusMessage);
            }
            catch (Exception exception)
            {
                SocketErrorStatus socketError = SocketError.GetStatus(exception.HResult);

                if (SocketError.GetStatus(exception.HResult) == SocketErrorStatus.Unknown)
                {
                    throw;
                }

                rootPage.NotifyUser(
                    "Error happened when receiving a datagram:" + exception.Message,
                    NotifyType.ErrorMessage);
            }
        }

        /// <summary>
        /// Notifies the user from a non-UI thread
        /// </summary>
        /// <param name="strMessage">The message</param>
        /// <param name="type">The type of notification</param>
        private void NotifyUserFromAsyncThread(string strMessage, NotifyType type)
        {
            var ignore = Dispatcher.RunAsync(
                CoreDispatcherPriority.Normal, () => rootPage.NotifyUser(strMessage, type));
        }
    }
}
