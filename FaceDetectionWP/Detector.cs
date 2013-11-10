using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Diagnostics;
using System.Windows;
using System.Runtime.InteropServices;
using System.IO;
using System.Windows.Media.Imaging;

namespace FaceDetectionWinPhone
{

    /// <summary>
    /// Class that performs face detection (this is the one you care about!)
    /// </summary>
    public class Detector
    {
        NativeFaceDetector.Detector m_detector;

        /// <summary>
        /// Factory method to create face detectors
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static Detector Create(String path)
        {
            try
            {
                return new Detector(XDocument.Load(path));
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
                return null;
            }
        }

        /// <summary>
        /// Construct the detector given an XML document describing the model file.
        /// This method parses the XML file and builds the feature medel
        /// </summary>
        /// <param name="document"></param>
        public Detector(XDocument document)
        {
            var root = document.Root.Elements().First();
            Debug.Assert(root != null, "xml document root is not haarcascade_frontalface_alt");

            // Get the size of the classifier (size of the region to look at, i.e. 20 x 20
            string[] sizeStr = (from node in root.Descendants()
                                where node.Name.LocalName == "size"
                                select node.Value.Trim().Split(' ')).First().ToArray();

            Point size = new System.Windows.Point(Convert.ToDouble(sizeStr[0], CultureInfo.InvariantCulture),
                                                  Convert.ToDouble(sizeStr[1], CultureInfo.InvariantCulture));


            m_detector = new NativeFaceDetector.Detector((int)size.X, (int)size.Y);
            var stagesRoot = root.Descendants().Where(x => x.Name.LocalName == "stages").First();
            var stages = stagesRoot.Elements();
            foreach (XElement stage in stages)
            {
                // There's an extra level for some reason so we have to do down one
                var trueStage = stage;
                float stage_threshold = (float)Convert.ToDouble(trueStage.Element("stage_threshold").Value.Trim(), CultureInfo.InvariantCulture);
                m_detector.addStage(stage_threshold);
                var trees = trueStage.Element("trees");
                foreach (XElement tree in trees.Elements())
                {
                    // There's an extra level for some reason so we have to do down one
                    XElement trueTree = tree.Elements().First();
                    XElement feature = trueTree.Element("feature");
                    float threshold = (float)Convert.ToDouble(trueTree.Element("threshold").Value.Trim(),CultureInfo.InvariantCulture);
                    int left_node = -1;
                    float left_val = 0;
                    int right_node = -1;
                    float right_val = 0;
                    XElement e = trueTree.Element("left_val");
                    if (e != null)
                    {
                        left_val = (float)Convert.ToDouble(e.Value.Trim(), CultureInfo.InvariantCulture);
                    }
                    else
                    {
                        left_node = Convert.ToInt32(trueTree.Element("left_node").Value.Trim(), CultureInfo.InvariantCulture);
                    }
                    e = trueTree.Element("right_val");
                    if (e != null)
                    {
                        right_val = (float)Convert.ToDouble(e.Value.Trim(), CultureInfo.InvariantCulture);
                    }
                    else
                    {
                        right_node = Convert.ToInt32(trueTree.Element("right_node").Value.Trim(), CultureInfo.InvariantCulture);
                    }
                    
                    m_detector.makeFeature(threshold, left_val, left_node, right_val, right_node, (int)size.X, (int)size.Y);
                    
                    var rects = feature.Element("rects");
                    foreach (var r in rects.Elements())
                    {
                        string rstr = r.Value.Trim();
                        RectFeature rect = RectFeature.fromString(rstr);
                        m_detector.addRect(rect.x1, rect.x2, rect.y1, rect.y2, rect.weight);
                    }

                    m_detector.addFeature();
                    m_detector.addTree();
                }
            }
        }

        /// <summary>
        /// Returns a list of rectangles representing detected objects from Viola-jones.
        /// 
        /// The algorithm tests, from sliding windows on the image at different scales which regions should be considered as searched objects.
        /// Please see Wikipedia for a description of the algorithm.
        /// </summary>
        /// <param name="file">the image file containing the image you want to detect</param>
        /// <param name="baseScale"> The initial ratio between the size of your image and the size of the sliding window (default: 2)</param>
        /// <param name="scale_inc">How much to increment your window for every iteration (default:1.25)</param>
        /// <param name="increment">How much to shif the window at each step, in terms of the % of the window size</param>
        /// <param name="min_neighbors"> Minimum number of overlapping face rectangles to be considered a valid face (default: 1)</param>
        /// <param name="multipleFaces"> Whether or not to detect multiple faces</param>
        public List<NativeFaceDetector.Rectangle> getFaces(String file, float baseScale, float scale_inc, float increment, int min_neighbors)
        {
            try
            {
                WriteableBitmap image = new WriteableBitmap(new BitmapImage(new Uri(file, UriKind.Absolute)));
                var result = getFaces(image, baseScale, scale_inc, increment, min_neighbors);
                return result;
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
            }
            return null;
        }

        /// <summary>
        /// Returns a list of rectangles representing detected objects from Viola-jones.
        /// 
        /// The algorithm tests, from sliding windows on the image at different scales which regions should be considered as searched objects.
        /// Please see Wikipedia for a description of the algorithm.
        /// </summary>
        /// <param name="image">the image you want to find stuff in</param>
        /// <param name="baseXcale"> The initial ratio between the size of your image and the size of the sliding window (default: 2)</param>
        /// <param name="scale_inc">How much to increment your window for every iteration (default:1.25)</param>
        /// <param name="increment">How much to shif the window at each step, in terms of the % of the window size</param>
        /// <param name="min_neighbors"> Minimum number of overlapping face rectangles to be considered a valid face (default: 1)</param>
        public List<NativeFaceDetector.Rectangle> getFaces(WriteableBitmap image, float baseScale, float scale_inc, float increment, int min_neighbors)
        {
            return getFaces(image.Pixels, image.PixelWidth, image.PixelHeight, baseScale, scale_inc, increment, min_neighbors);
        }

        /// <summary>
        /// Returns a list of rectangles representing detected objects from Viola-jones.
        /// 
        /// The algorithm tests, from sliding windows on the image at different scales which regions should be considered as searched objects.
        /// Please see Wikipedia for a description of the algorithm.
        /// </summary>
        /// <param name="imageData">int array of the image you want to find stuff in</param>
        /// <param name="baseXcale"> The initial ratio between the size of your image and the size of the sliding window (default: 2)</param>
        /// <param name="scale_inc">How much to increment your window for every iteration (default:1.25)</param>
        /// <param name="increment">How much to shif the window at each step, in terms of the % of the window size</param>
        /// <param name="min_neighbors"> Minimum number of overlapping face rectangles to be considered a valid face (default: 1)</param>
        public List<NativeFaceDetector.Rectangle> getFaces(int[] imageData, int width, int height, float baseScale, float scale_inc, float increment, int min_neighbors)
        {
            List<NativeFaceDetector.Rectangle> ret = new List<NativeFaceDetector.Rectangle>();
            m_detector.getFaces(ret, imageData, width, height, baseScale, scale_inc, increment, min_neighbors);
            return ret;
        }
    }
}
