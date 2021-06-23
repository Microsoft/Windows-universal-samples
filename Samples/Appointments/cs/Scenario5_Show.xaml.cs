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

using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using System;
using Windows.ApplicationModel.Appointments;
using System.Linq;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace SDKTemplate
{
    public sealed partial class Scenario5_Show : Page
    {
        private MainPage rootPage = MainPage.Current;

        public Scenario5_Show()
        {
            this.InitializeComponent();
            this.DataContext = this;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            var ignore = AppointmentGet();
        }

        private async Task AppointmentGet()
        {
            var dateToShow = DateTime.Now.AddDays(1);
            var duration = TimeSpan.FromHours(24);

            AppointmentStore calendar = await AppointmentManager.RequestStoreAsync(AppointmentStoreAccessType.AllCalendarsReadOnly);

            // Specify which values to retrieve
            FindAppointmentsOptions options = new FindAppointmentsOptions();
            options.FetchProperties.Add(AppointmentProperties.Subject);
            options.FetchProperties.Add(AppointmentProperties.StartTime);
            options.FetchProperties.Add(AppointmentProperties.Duration);
            options.FetchProperties.Add(AppointmentProperties.AllDay);
            options.FetchProperties.Add(AppointmentProperties.Details);
            options.FetchProperties.Add(AppointmentProperties.DetailsKind);

            var iteratingAppointments = await calendar.FindAppointmentsAsync(dateToShow, duration, options);
            var items = from a in iteratingAppointments select a;

            await rootPage.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, 
                () => { items.ToList().ForEach(a => this.appointments.Add(a)); }
                );
        }

        private ObservableCollection<Appointment> appointments = new ObservableCollection<Appointment>();
        public ObservableCollection<Appointment> Appointments => appointments;

        /// <summary>
        /// Show the default appointment provider at a point in time 24 hours from now.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void Show_Click(object sender, RoutedEventArgs e)
        {
            var dateToShow = DateTime.Now.AddDays(1);
            var duration = TimeSpan.FromHours(24);

            await Windows.ApplicationModel.Appointments.AppointmentManager.ShowTimeFrameAsync(dateToShow, duration);
            rootPage.NotifyUser("The default appointments provider should have appeared on screen.", NotifyType.StatusMessage);
        }
    }
}
