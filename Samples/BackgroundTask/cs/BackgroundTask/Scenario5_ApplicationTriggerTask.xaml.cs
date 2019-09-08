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
using SDKTemplate;
using Windows.ApplicationModel.Background;
using Windows.Storage;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace SDKTemplate
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ApplicationTriggerTask : Page
    {
        // A pointer back to the main page.  This is needed if you want to call methods in MainPage such
        // as NotifyUser()
        MainPage rootPage = MainPage.Current;

        // A pointer to the ApplicationTrigger so we can signal it later
        ApplicationTrigger trigger = null;

        public ApplicationTriggerTask()
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
            foreach (var task in BackgroundTaskRegistration.AllTasks)
            {
                if (task.Value.Name == BackgroundTaskSample.ApplicationTriggerTaskName)
                {
                    AttachProgressAndCompletedHandlers(task.Value);
                    BackgroundTaskSample.UpdateBackgroundTaskRegistrationStatus(BackgroundTaskSample.ApplicationTriggerTaskName, true);
                    trigger = new ApplicationTrigger();
                    break;
                }
            }

            UpdateUI();
        }

        /// <summary>
        /// Register a ApplicationTriggerTask.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RegisterBackgroundTask(object sender, RoutedEventArgs e)
        {
            trigger = new ApplicationTrigger();
            var task = BackgroundTaskSample.RegisterBackgroundTask(BackgroundTaskSample.SampleBackgroundTaskEntryPoint,
                                                                   BackgroundTaskSample.ApplicationTriggerTaskName,
                                                                   trigger,
                                                                   null);
            AttachProgressAndCompletedHandlers(task);
            UpdateUI();
        }

        /// <summary>
        /// Unregister a ApplicationTriggerTask.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UnregisterBackgroundTask(object sender, RoutedEventArgs e)
        {
            BackgroundTaskSample.UnregisterBackgroundTasks(BackgroundTaskSample.ApplicationTriggerTaskName);
            BackgroundTaskSample.ApplicationTriggerTaskResult = "";
            trigger = null;
            UpdateUI();
        }

        /// <summary>
        /// Signal a ApplicationTriggerTask.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void SignalBackgroundTask(object sender, RoutedEventArgs e)
        {
            // Reset the completion status
            var settings = ApplicationData.Current.LocalSettings;
            settings.Values.Remove(BackgroundTaskSample.ApplicationTriggerTaskName);

            //Signal the ApplicationTrigger
            var result = await trigger.RequestAsync();
            BackgroundTaskSample.ApplicationTriggerTaskResult = "Signal result: " + result.ToString();
            UpdateUI();
        }

        /// <summary>
        /// Attach progress and completed handers to a background task.
        /// </summary>
        /// <param name="task">The task to attach progress and completed handlers to.</param>
        private void AttachProgressAndCompletedHandlers(IBackgroundTaskRegistration task)
        {
            task.Progress += new BackgroundTaskProgressEventHandler(OnProgress);
            task.Completed += new BackgroundTaskCompletedEventHandler(OnCompleted);
        }

        /// <summary>
        /// Handle background task progress.
        /// </summary>
        /// <param name="task">The task that is reporting progress.</param>
        /// <param name="e">Arguments of the progress report.</param>
        private void OnProgress(IBackgroundTaskRegistration task, BackgroundTaskProgressEventArgs args)
        {
            var ignored = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                var progress = "Progress: " + args.Progress + "%";
                BackgroundTaskSample.ApplicationTriggerTaskProgress = progress;
                UpdateUI();
            });
        }

        /// <summary>
        /// Handle background task completion.
        /// </summary>
        /// <param name="task">The task that is reporting completion.</param>
        /// <param name="e">Arguments of the completion report.</param>
        private void OnCompleted(IBackgroundTaskRegistration task, BackgroundTaskCompletedEventArgs args)
        {
            UpdateUI();
        }

        /// <summary>
        /// Update the scenario UI.
        /// </summary>
        private async void UpdateUI()
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
            () =>
            {
                RegisterButton.IsEnabled = !BackgroundTaskSample.ApplicationTriggerTaskRegistered;
                UnregisterButton.IsEnabled = BackgroundTaskSample.ApplicationTriggerTaskRegistered;
                SignalButton.IsEnabled = BackgroundTaskSample.ApplicationTriggerTaskRegistered && (trigger != null);
                Progress.Text = BackgroundTaskSample.ApplicationTriggerTaskProgress;
                Result.Text = BackgroundTaskSample.ApplicationTriggerTaskResult;
                Status.Text = BackgroundTaskSample.GetBackgroundTaskStatus(BackgroundTaskSample.ApplicationTriggerTaskName);
            });
        }

    }
}
