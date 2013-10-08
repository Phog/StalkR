using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Media;
using System.Windows.Controls;
using System.Windows.Navigation;
using System.Windows.Threading;
using System.Windows.Media.Imaging;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Microsoft.Devices;
using Microsoft.Xna.Framework.Media;
using FaceDetectionWinPhone;
using StalkR.Resources;

namespace StalkR
{
    public partial class MainPage : PhoneApplicationPage
    {
        private const double EPSILON = 0.00001;
        enum Mode { Preview, Capture, Display };

        Mode mode;
        PhotoCamera camera;
        MediaLibrary mediaLibrary;
        Detector detector;

        public MainPage()
        {
            InitializeComponent();
            previewMode();

            camera       = null;
            mediaLibrary = new MediaLibrary();
            detector     = Detector.Create("haarcascade_frontalface_alt.xml");

            previewTransform.Rotation = 90;
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            camera = new Microsoft.Devices.PhotoCamera(CameraType.Primary);
            camera.Initialized           += new EventHandler<Microsoft.Devices.CameraOperationCompletedEventArgs>(Camera_Initialized);
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

        private void displayMode(BitmapImage bitmap)
        {
            mode = Mode.Display;
            infoBox.Text = "Detecting faces... ";
            IdentifyButton.Content = "Cancel";

            displayBrush.ImageSource  = bitmap;
            displayTransform.Rotation = 90;
            displayCanvas.Visibility  = Visibility.Visible;

            this.Dispatcher.BeginInvoke(delegate()
            {
                // The user sees a transposed image in the viewfinder, transpose the image for face detection as well.
                WriteableBitmap detectorBitmap = (new WriteableBitmap(bitmap)).Rotate(90);
                List<Rectangle> faces = detector.getFaces(detectorBitmap, 2.5f, 1.08f, 0.05f, 2, true);

                if (faces.Count > 0)
                {
                    using (detectorBitmap.GetBitmapContext())
                    {
                        foreach (Rectangle face in faces)
                        {
                            // The facedetector works with the transposed image, correct for that.
                            int width = Convert.ToInt32(face.Width);
                            int height = Convert.ToInt32(face.Height);
                            int x = Convert.ToInt32(face.X);
                            int y = Convert.ToInt32(face.Y);

                            detectorBitmap.DrawRectangle(x, y, x + height, y + width, System.Windows.Media.Colors.Green);
                        }
                    }
                    infoBox.Text = "Face(s) detected";
                }
                else
                {
                    infoBox.Text = "No faces in picture";
                }

                displayTransform.Rotation = 0;
                displayBrush.ImageSource  = detectorBitmap;
            });
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

        void Camera_Initialized(object sender, Microsoft.Devices.CameraOperationCompletedEventArgs e)
        {
            if (e.Succeeded)
                camera.Resolution = camera.AvailableResolutions.First();
            else
                camera = null;
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