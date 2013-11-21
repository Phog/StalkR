using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using StalkRTests.Resources;
using System.Windows.Media.Imaging;
using NativeFaceDetector;
using Windows.Storage;
using System.Threading.Tasks;
using System.IO;


namespace StalkRTests
{
    public partial class MainPage : PhoneApplicationPage
    {
        const int NUM_CATEGORIES = 4;
        const int NUM_FACES      = 10;

        private void dumpCSV(int numFaces, double[] durations)
        {
            String fileText = String.Empty;
            for (int i = 0; i < durations.Length; ++i)
                fileText += String.Format("\"{0}\",", durations[i]);

            StorageFolder folder = ApplicationData.Current.LocalFolder;
            using (var writer = new StreamWriter(folder.Path + String.Format(@"\{0}.csv", numFaces)))
            {
                writer.WriteLine(fileText);
            }
        }

        public MainPage()
        {
            InitializeComponent();
            FaceDetectionWinPhone.Detector detector = FaceDetectionWinPhone.Detector.Create("haarcascade_frontalface_default.xml");

            // Warmup, we want the CPU frequency scaling to kick in and maximize our clock speeds.
            {
                WriteableBitmap[] testBitmaps = new WriteableBitmap[NUM_FACES];
                for (int j = 0; j < NUM_FACES; j++)
                {
                    var resource = App.GetResourceStream(new Uri(String.Format("Assets/{0}/{1}.jpg", 1, j), UriKind.Relative));
                    BitmapImage testBitmap = new BitmapImage();
                    testBitmap.SetSource(resource.Stream);
                    testBitmaps[j] = new WriteableBitmap(testBitmap);
                }

                var thread = new System.Threading.Thread(delegate()
                {
                    for (int j = 0; j < NUM_FACES; j++)
                        detector.getFaces(testBitmaps[j], 3.0f, 1.15f, 0.08f, 2);
                });
                thread.Start();
                thread.Join();
            }

            for (int i = 1; i <= NUM_CATEGORIES; i++)
            {
                // Collect garbage outside of our timing loop.
                System.GC.Collect();
                System.GC.WaitForPendingFinalizers();

                WriteableBitmap[] testBitmaps = new WriteableBitmap[NUM_FACES];
                double[] testSpeed = new double[NUM_FACES];

                for (int j = 0; j < NUM_FACES; j++)
                {
                    var resource = App.GetResourceStream(new Uri(String.Format("Assets/{0}/{1}.jpg", i, j), UriKind.Relative));
                    BitmapImage testBitmap = new BitmapImage();
                    testBitmap.SetSource(resource.Stream);
                    testBitmaps[j] = new WriteableBitmap(testBitmap);
                }

                var thread = new System.Threading.Thread(delegate()
                {
                    for (int j = 0; j < NUM_FACES; j++)
                    {
                        DateTime before = DateTime.Now;
                        List<Rectangle> faces = detector.getFaces(testBitmaps[j], 3.0f, 1.15f, 0.08f, 2);
                        testSpeed[j] = (DateTime.Now - before).TotalMilliseconds;
                    }
                });
                thread.Start();
                thread.Join();

                double average = 0.0, min = 10000000.0, max = -10000000.0;
                for (int j = 0; j < NUM_FACES; j++)
                {
                    average += testSpeed[j];
                    min = Math.Min(min, testSpeed[j]);
                    max = Math.Max(max, testSpeed[j]);
                }
                average /= NUM_FACES;
                OutputBlock.Text += String.Format("Number of faces: {0}\nAverage ms: {1}\nMax ms: {2}\nMin ms: {3}\n=============\n",
                                                  i, average, max, min);
                dumpCSV(i, testSpeed);
            }
        }

    }
}