using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using System.Windows.Threading;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using StalkR.Resources;
    
namespace StalkR
{
    public partial class MainPage : PhoneApplicationPage
    {
        DispatcherTimer eventTimer;

        public MainPage()
        {
            InitializeComponent();

            eventTimer          = new DispatcherTimer();
            eventTimer.Interval = TimeSpan.FromSeconds(1.0);
            eventTimer.Tick    += EventTimer_Tick;
        }

        void EventTimer_Tick(Object sender, EventArgs args)
        {
            eventTimer.Stop();
            infoBox.Text = "";
        }

        private void IdentifyButton_Click(object sender, RoutedEventArgs e)
        {
            infoBox.Text = "Crunching numbers...";
            eventTimer.Start();
        }
    }
}