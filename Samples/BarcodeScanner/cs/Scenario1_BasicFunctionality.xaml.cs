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

using System;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using Windows.Devices.PointOfService;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace SDKTemplate
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Scenario1_BasicFunctionality : Page
    {
        // A pointer back to the main page.  This is needed if you want to call methods in MainPage such
        // as NotifyUser()
        MainPage rootPage = MainPage.Current;

        BarcodeScanner scanner = null;
        ClaimedBarcodeScanner claimedScanner = null;

        public Scenario1_BasicFunctionality()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Invoked when this page is about to be displayed in a Frame.
        /// </summary>
        /// <param name="e">Event data that describes how this page was reached.  The Parameter
        /// property is typically used to configure the page.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            ResetTheScenarioState();
        }

        /// <summary>
        /// Invoked when this page is no longer displayed.
        /// </summary>
        /// <param name="e">Event data that describes how this page was exited.  The Parameter
        /// property is typically used to configure the page.</param>
        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            ResetTheScenarioState();
        }

        /// <summary>
        /// Event Handler for Start Scan Button Click.
        /// Sets up the barcode scanner to be ready to receive the data events from the scan.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void ScenarioStartScanButton_Click(object sender, RoutedEventArgs e)
        {
            ScenarioStartScanButton.IsEnabled = false;

            rootPage.NotifyUser("Acquiring barcode scanner object.", NotifyType.StatusMessage);

            // Acquire the barcode scanner.
            scanner = await DeviceHelpers.GetFirstBarcodeScannerAsync();
            if (scanner != null)
            {
                // after successful creation, claim the scanner for exclusive use and enable it so that data reveived events are received.
                claimedScanner = await scanner.ClaimScannerAsync();

                if (claimedScanner != null)
                {
                    // It is always a good idea to have a release device requested event handler. If this event is not handled, there are chances of another app can
                    // claim ownership of the barcode scanner.
                    claimedScanner.ReleaseDeviceRequested += claimedScanner_ReleaseDeviceRequested;

                    // after successfully claiming, attach the datareceived event handler.
                    claimedScanner.DataReceived += claimedScanner_DataReceived;

                    // Ask the API to decode the data by default. By setting this, API will decode the raw data from the barcode scanner and
                    // send the ScanDataLabel and ScanDataType in the DataReceived event
                    claimedScanner.IsDecodeDataEnabled = true;

                    // enable the scanner.
                    // The scanner must be enabled in order to receive the DataReceived event.
                    await claimedScanner.EnableAsync();
                    
                    // Begin scanning
                    await claimedScanner.StartSoftwareTriggerAsync();

                    rootPage.NotifyUser("Ready to scan. Device ID: " + claimedScanner.DeviceId, NotifyType.StatusMessage);
                    ScenarioEndScanButton.IsEnabled = true;
                }
                else
                {
                    rootPage.NotifyUser("Claim barcode scanner failed.", NotifyType.ErrorMessage);
                    ScenarioStartScanButton.IsEnabled = true;
                }
            }
            else
            {
                rootPage.NotifyUser("Barcode scanner not found. Please connect a barcode scanner.", NotifyType.ErrorMessage);
                ScenarioStartScanButton.IsEnabled = true;
            }
        }

        /// <summary>
        /// Event handler for the Release Device Requested event fired when barcode scanner receives Claim request from another application
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"> Contains the ClamiedBarcodeScanner that is sending this request</param>
        void claimedScanner_ReleaseDeviceRequested(object sender, ClaimedBarcodeScanner e)
        {
            // always retain the device
            e.RetainDevice();

            rootPage.NotifyUser("Event ReleaseDeviceRequested received. Retaining the barcode scanner.", NotifyType.StatusMessage);
        }

        /// <summary>
        /// Event handler for the DataReceived event fired when a barcode is scanned by the barcode scanner
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"> Contains the BarcodeScannerReport which contains the data obtained in the scan</param>
        async void claimedScanner_DataReceived(ClaimedBarcodeScanner sender, BarcodeScannerDataReceivedEventArgs args)
        {
            // need to update the UI data on the dispatcher thread.
            // update the UI with the data received from the scan.
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                // read the data from the buffer and convert to string.
                ScenarioOutputScanDataLabel.Text = DataHelpers.GetDataLabelString(args.Report.ScanDataLabel, args.Report.ScanDataType);
                ScenarioOutputScanData.Text = DataHelpers.GetDataString(args.Report.ScanData);
                ScenarioOutputScanDataType.Text = BarcodeSymbologies.GetName(args.Report.ScanDataType);
            });
        }

        /// <summary>
        /// Reset the Scenario state
        /// </summary>
        private void ResetTheScenarioState()
        {
            if (claimedScanner != null)
            {
                // Detach the event handlers
                claimedScanner.DataReceived -= claimedScanner_DataReceived;
                claimedScanner.ReleaseDeviceRequested -= claimedScanner_ReleaseDeviceRequested;
                // Release the Barcode Scanner and set to null
                claimedScanner.Dispose();
                claimedScanner = null;
            }

            if (scanner != null)
            {
                scanner.Dispose();
                scanner = null;
            }

            // Reset the UI if we are still the current page.
            if (Frame.Content == this)
            {
                rootPage.NotifyUser("Click the start scanning button to begin.", NotifyType.StatusMessage);
                this.ScenarioOutputScanData.Text = "No data";
                this.ScenarioOutputScanDataLabel.Text = "No data";
                this.ScenarioOutputScanDataType.Text = "No data";

                // reset the button state
                ScenarioEndScanButton.IsEnabled = false;
                ScenarioStartScanButton.IsEnabled = true;
            }
        }

        /// <summary>
        /// Event handler for End Scan Button Click.
        /// Releases the Barcode Scanner and resets the text in the UI
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ScenarioEndScanButton_Click(object sender, RoutedEventArgs e)
        {
            // reset the scenario state
            this.ResetTheScenarioState();
        }

    }
}
