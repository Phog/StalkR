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
using System.Windows.Input;

namespace StalkR
{
    public partial class MainPage : PhoneApplicationPage
    {
        private const double EPSILON = 0.00001;

        PhotoCamera camera;
        MediaLibrary mediaLibrary;
        FaceDetectionWinPhone.Detector detector;
        FaceRecognizer recognizer;
        DateTime frameStart;

        public MainPage()
        {
            InitializeComponent();
            camera       = null;
            mediaLibrary = new MediaLibrary();
            detector     = FaceDetectionWinPhone.Detector.Create("haarcascade_frontalface_default.xml");
            recognizer   = new FaceRecognizer();

            overlayCanvas.MouseLeftButtonDown += Preview_Click;
            previewTransform.Rotation = 90;
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            Microsoft.Devices.PhotoCamera tempCamera = new Microsoft.Devices.PhotoCamera(CameraType.Primary);
            tempCamera.Initialized += new EventHandler<Microsoft.Devices.CameraOperationCompletedEventArgs>(Camera_Initialized);
            previewBrush.SetSource(tempCamera);
        }

        protected override void OnNavigatingFrom(System.Windows.Navigation.NavigatingCancelEventArgs e)
        {
            if (camera == null)
                return;

            camera.Dispose();
            camera.Initialized -= Camera_Initialized;
            camera = null;
        }

        // Assumes that the bitmapcontext is active
        private void drawHeaderForBox(WriteableBitmap bitmap, int x, int y, int width, String text)
        {
            const int BOX_HEIGHT = 40;
            const int BOX_WIDTH  = 120;

            TranslateTransform transform = new TranslateTransform();
            transform.X = x + (width - BOX_WIDTH) / 2;
            transform.Y = y;

            Border border = new Border();
            border.Height = BOX_HEIGHT;
            border.Width = BOX_WIDTH;
            border.Background = new SolidColorBrush(Color.FromArgb(128, 0, 0, 0));

            TextBlock textBlock = new TextBlock();
            textBlock.FontSize = 36;
            textBlock.TextAlignment = TextAlignment.Center;
            textBlock.Foreground = new SolidColorBrush(Colors.White);
            textBlock.Text = text;

            border.Child = textBlock;
            border.Arrange(new Rect(0.0, 0.0, border.Width, border.Height));
            border.UpdateLayout();

            bitmap.Render(border, transform);
        }

        private void detectFaces(WriteableBitmap bitmap)
        {
            // The user sees a transposed image in the viewfinder, transpose the image for face detection as well.
            WriteableBitmap detectorBitmap = (new WriteableBitmap(bitmap)).Rotate(90);
            var thread = new System.Threading.Thread(delegate()
            {
                List<Rectangle> rectangles = detector.getFaces(detectorBitmap, 3.0f, 1.15f, 0.08f, 2);
                this.Dispatcher.BeginInvoke(delegate()
                {
                    recognizer.newFrame(rectangles, detectorBitmap);
                    if (camera == null)
                        return;

                    WriteableBitmap rectBitmap = new WriteableBitmap(detectorBitmap.PixelWidth, detectorBitmap.PixelHeight);
                    using(rectBitmap.GetBitmapContext())
                    {
                        rectBitmap.Clear(Colors.Transparent);

                        foreach (Face face in recognizer.faces)
                        {
                            Rectangle rect = face.rectangle;
                            int width = Convert.ToInt32(rect.width());
                            int height = Convert.ToInt32(rect.height());
                            int x = Convert.ToInt32(rect.x());
                            int y = Convert.ToInt32(rect.y());

                            String text = "...";
                            if (face.response != null)
                                text = face.response.friend == null || face.response.friend.first_name == String.Empty
                                     ? "?" : face.response.friend.first_name;

                            drawHeaderForBox(rectBitmap, x, y - 20, width, text);
                        }

                        double milliseconds = (DateTime.Now - frameStart).TotalMilliseconds;
                        drawHeaderForBox(rectBitmap, 2, 0, 140, String.Format("{0} ms", Math.Floor(milliseconds)));
                        frameStart = DateTime.Now;
                        rectBitmap.Invalidate();
                    }

                    recognizer.recognize(username.Text, password.Password, ipAddress.Text);
                    overlayBrush.ImageSource = rectBitmap;

                    camera.GetPreviewBufferArgb32(bitmap.Pixels);
                    detectFaces(bitmap);
                });
            });
            thread.Start();
        }

        private void showDetails(Face face)
        {
            if (face == null || face.response == null)
                return;

            panoramaRoot.DefaultItem = (PanoramaItem)panoramaRoot.Items[2];
            resultImage.Source       = face.image;
            if (!String.IsNullOrEmpty(face.response.error))
            {
                resultText.Text = face.response.error;
                return;
            }

            Friend friend = face.response.friend;
            resultText.Text = String.Format("{0} {1}\n{2}\n{3}", friend.first_name, 
                                            friend.last_name, friend.phone, friend.email);
        }

        private void Preview_Click(object sender, MouseButtonEventArgs e)
        {
            Canvas canvas = (Canvas)sender;

            double ratio = camera.PreviewResolution.Width / canvas.ActualHeight;
            Point p = e.GetPosition(canvas);

            int y = (int)(p.Y * ratio);
            int x = (int)(p.X * ratio + (camera.PreviewResolution.Height - canvas.ActualWidth * ratio) / 2.0);

            showDetails(recognizer.selectFace(x, y));
        }

        void Camera_Initialized(object sender, Microsoft.Devices.CameraOperationCompletedEventArgs e)
        {
            if (!e.Succeeded)
            {
                camera = null;
                return;
            }

            try
            {
                camera = (Microsoft.Devices.PhotoCamera)sender;
                camera.Resolution = camera.AvailableResolutions.First();
            }
            catch (Exception)
            {
                camera = null;
                return;
            }

            this.Dispatcher.BeginInvoke(delegate()
            {
                if (camera == null)
                    return;

                WriteableBitmap bitmap = new WriteableBitmap((int)camera.PreviewResolution.Width,
                                                             (int)camera.PreviewResolution.Height);
                frameStart = DateTime.Now;
                camera.GetPreviewBufferArgb32(bitmap.Pixels);
                detectFaces(bitmap);
            });
        }
    }
}