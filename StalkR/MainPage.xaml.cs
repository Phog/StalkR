using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Shapes;
using System.Windows.Media;
using System.Windows.Controls;
using System.Windows.Navigation;
using System.Windows.Threading;
using System.Windows.Media.Imaging;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Microsoft.Devices;
using Microsoft.Xna.Framework.Media;
using StalkR.Resources;

namespace StalkR
{
    public partial class MainPage : PhoneApplicationPage
    {
        enum Mode { Preview, Capture, Display };

        Mode mode;
        PhotoCamera camera;
        MediaLibrary mediaLibrary;

        public MainPage()
        {
            InitializeComponent();
            previewMode();

            camera       = null;
            mediaLibrary = new MediaLibrary();

            displayTransform.Rotation = 90;
            previewTransform.Rotation = 90;
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            camera = new Microsoft.Devices.PhotoCamera(CameraType.Primary);
            camera.CaptureImageAvailable += new EventHandler<Microsoft.Devices.ContentReadyEventArgs>(Camera_CaptureImageAvailable);
            previewBrush.SetSource(camera);
        }

        protected override void OnNavigatingFrom(System.Windows.Navigation.NavigatingCancelEventArgs e)
        {
            if (camera == null)
                return;

            camera.Dispose();
            camera.CaptureImageAvailable -= Camera_CaptureImageAvailable;
        }

        private void previewMode()
        {
            mode = Mode.Preview;
            displayCanvas.Visibility = Visibility.Collapsed;
            hideRectangle();

            infoBox.Text = "";
            IdentifyButton.Content = "Identify";
        }

        private void captureMode()
        {
            if (camera == null)
                return;

            mode = Mode.Capture;
            infoBox.Text = "Taking picture...";
            camera.CaptureImage();
        }

        private void hideRectangle()
        {
            faceRectangle.Visibility = Visibility.Collapsed;
        }

        private void drawRectangle(int x, int y, int width, int height)
        {
            faceRectangle.RenderTransform = new TranslateTransform() { X = x, Y = y };
            faceRectangle.Height          = height;
            faceRectangle.Width           = width;
            faceRectangle.Visibility      = Visibility.Visible;
        }

        private void displayMode(BitmapImage bitmap)
        {
            mode = Mode.Display;
            infoBox.Text = "Crunching numbers...";
            IdentifyButton.Content = "Cancel";

            displayBrush.ImageSource = bitmap;
            displayCanvas.Visibility = Visibility.Visible;
            drawRectangle(0, 0, 300, 250);
        }

        private void IdentifyButton_Click(object sender, RoutedEventArgs e)
        {
            if (mode == Mode.Preview)
            {
                captureMode();
                return;
            }

            if (mode == Mode.Display)
            {
                previewMode();
                return;
            }
        }

        void Camera_CaptureImageAvailable(object sender, Microsoft.Devices.ContentReadyEventArgs e)
        {
            this.Dispatcher.BeginInvoke(delegate()
            {
                try
                {
                    BitmapImage bitmap = new BitmapImage();
                    bitmap.SetSource(e.ImageStream);
                    displayMode(bitmap);
                }
                finally
                {
                    // Close image stream
                    e.ImageStream.Close();
                }
            });
        }
    }
}