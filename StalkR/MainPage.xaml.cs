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
using NativeFaceDetector;
using StalkR.Resources;
using System.IO;

namespace StalkR
{
    public partial class MainPage : PhoneApplicationPage
    {
        private const double EPSILON = 0.00001;
        private ListBox[] faceLists;
        enum Mode { Preview, Capture };

        Mode mode;
        PhotoCamera camera;
        MediaLibrary mediaLibrary;
        FaceDetectionWinPhone.Detector detector;

        public MainPage()
        {
            InitializeComponent();
            previewMode();

            faceLists    = new ListBox[] { faceList0, faceList1, faceList2 };
            camera       = null;
            mediaLibrary = new MediaLibrary();
            detector     = FaceDetectionWinPhone.Detector.Create("haarcascade_frontalface_default.xml");

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
            IdentifyButton.Content = "Identify";
        }

        private void captureMode()
        {
            if (camera == null)
                return;

            mode = Mode.Capture;
            camera.CaptureImage();
        }

        private void detectFaces(BitmapImage bitmap)
        {
            faceBar.Visibility = Visibility.Visible;
            foreach(ListBox faceList in faceLists)
                faceList.Items.Clear();

            // The user sees a transposed image in the viewfinder, transpose the image for face detection as well.
            WriteableBitmap detectorBitmap = (new WriteableBitmap(bitmap)).Rotate(90);
            var thread = new System.Threading.Thread(delegate()
            {
                List<Rectangle> faces = detector.getFaces(detectorBitmap, 3.0f, 1.15f, 0.08f, 2);
                this.Dispatcher.BeginInvoke(delegate()
                {
                    faceBar.Visibility = Visibility.Collapsed;

                    if (faces.Count > 0)
                    {
                        for (int i = 0; i < faces.Count(); i++)
                        {
                            Rect face = new Rect(faces[i].x(), faces[i].y(), faces[i].width(), faces[i].height());
                            WriteableBitmap croppedFace = detectorBitmap.Crop(face);
                            faceLists[i % 3].Items.Add(detectorBitmap.Crop(face));
                        }
                    }
                });
            });
            thread.Start();
        }

        private void showResults(Response response)
        {
            this.Dispatcher.BeginInvoke(delegate()
            {
                resultBar.Visibility   = Visibility.Collapsed;
                resultImage.Visibility = Visibility.Visible;
                if (!String.IsNullOrEmpty(response.error))
                {
                    resultText.Text = response.error;
                    return;
                }

                Friend friend = response.friend;
                resultText.Text = String.Format("{0} {1}", friend.first_name, friend.last_name);
            });
        }


        private void IdentifyButton_Click(object sender, RoutedEventArgs e)
        {
            if (mode == Mode.Preview)
            {
                captureMode();
                panoramaRoot.DefaultItem = (PanoramaItem)panoramaRoot.Items[2];
                return;
            }
        }

        private void Image_Click(object sender, RoutedEventArgs e)
        {
            resultImage.Visibility   = Visibility.Collapsed;
            resultText.Text          = String.Empty;
            resultBar.Visibility     = Visibility.Visible;
            panoramaRoot.DefaultItem = (PanoramaItem)panoramaRoot.Items[3];

            Image image            = (Image) sender;
            WriteableBitmap bitmap = (WriteableBitmap) image.Source;
            resultImage.Source     = bitmap;

            Dictionary<String, object> parameters = new Dictionary<string, object>();
            parameters["username"] = username.Text;
            parameters["password"] = password.Password;

            MemoryStream imageStream = new MemoryStream();
            bitmap.SaveJpeg(imageStream, 256, 256, 0, 100);
            parameters["image"] = imageStream.ToArray();

            String url = "http://" + ipAddress.Text + "/recognize";
            PostRequest request = new PostRequest(url, parameters, showResults);
            request.submit();
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
                    detectFaces(bitmap);
                    previewMode();
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