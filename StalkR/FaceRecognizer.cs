using NativeFaceDetector;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace StalkR
{
    class Face
    {
        public Rectangle rectangle;
        public Response response;
        public int timestamp;
        public WriteableBitmap image;
        public bool responsePending;

        public Face(Rectangle r, int t, WriteableBitmap i)
        {
            response        = null;
            responsePending = false;
            update(r, t, i);
        }

        public void update(Rectangle r, int t, WriteableBitmap i)
        {
            rectangle = r;
            timestamp = t;
            image     = i.Crop(new Rect(r.x(), r.y(), r.width(), r.height()));
        }
    }

    class FaceRecognizer
    {
        public List<Face> faces { get; private set; }
        private int timestamp;

        public FaceRecognizer()
        {
            faces = new List<Face>();
            timestamp = 0;
        }

        public void newFrame(List<Rectangle> rectangles, WriteableBitmap image)
        {
            timestamp++;

            List<Rectangle> newRectangles = new List<Rectangle>();
            foreach (Rectangle rectangle in rectangles)
            {
                double bestDistance = 0.0;
                Face bestFace = null;
                foreach (Face face in faces)
                {
                    double distance = rectangle.distanceTo(face.rectangle);
                    if (bestFace == null || bestDistance > distance)
                    {
                        bestFace     = face;
                        bestDistance = distance;
                    }
                }

                if (bestFace != null && bestDistance <= 0.5 * rectangle.diagonal())
                    bestFace.update(rectangle, timestamp, image);
                else
                    newRectangles.Add(rectangle);
            }

            List<Face> removeList = new List<Face>();
            foreach (Face face in faces)
            {
                if (face.timestamp != timestamp)
                    removeList.Add(face);
            }

            foreach (Face face in removeList)
                faces.Remove(face);

            foreach (Rectangle rectangle in newRectangles)
            {
                faces.Add(new Face(rectangle, timestamp, image));
            }
        }

        public void recognize(String username, String password, String ipAddress)
        {
            foreach (Face face in faces)
            {
                if (face.responsePending || 
                    (face.response != null && String.IsNullOrEmpty(face.response.error)))
                    continue;

                try
                {
                    Dictionary<String, object> parameters = new Dictionary<string, object>();
                    parameters["username"] = username;
                    parameters["password"] = password;

                    MemoryStream imageStream = new MemoryStream();
                    face.image.SaveJpeg(imageStream, 256, 256, 0, 100);
                    parameters["image"] = imageStream.ToArray();

                    String url = "http://" + ipAddress + "/recognize";
                    PostRequest request = new PostRequest(url, parameters, delegate(Response response)
                    {
                        face.response        = response;
                        face.responsePending = false;
                    });

                    request.submit();
                    face.responsePending = true;
                }
                catch(Exception)
                {
                }
            }
        }

        public Face selectFace(int x, int y)
        {
            foreach (Face face in faces)
            {
                if (face.rectangle.contains(x, y))
                    return face;
            }

            return null;
        }
    }
}
