using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Runtime.Serialization.Json;

namespace StalkR
{
    /*
     *  Modified from http://stackoverflow.com/questions/11423876/post-an-image-using-httpwebrequest-with-json-response-wp7
     */
    public class PostRequest
    {
        public delegate void CallBack(Response response);
        private string url, boundary;
        private Response response;
        private CallBack callBack;
        private Dictionary<string, object> parameters;
        private HttpWebRequest request;

        public PostRequest(String url, Dictionary<string, object> parameters, CallBack callBack)
        {
            this.url = url;
            this.boundary = "----------" + DateTime.Now.Ticks.ToString();
            this.parameters = parameters;
            this.callBack = callBack;
        }

        public void submit()
        {
            request = WebRequest.CreateHttp(url);
            request.Method = "POST";
            request.ContentType = "multipart/form-data; boundary=" + boundary;
            request.BeginGetRequestStream(new AsyncCallback(requestReady), request);
        }

        private void requestReady(IAsyncResult asynchronousResult)
        {
            using (Stream postStream = request.EndGetRequestStream(asynchronousResult))
            {
                writeMultipartObject(postStream, parameters);
            }

            request.BeginGetResponse(new AsyncCallback(responseReady), request);
        }

        private void responseReady(IAsyncResult asynchronousResult)
        {
            try
            {
                using (var httpResponse = (HttpWebResponse)request.EndGetResponse(asynchronousResult))
                using (var streamResponse = httpResponse.GetResponseStream())
                {
                    DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(Response));
                    response = (Response)serializer.ReadObject(streamResponse);
                }
            }
            catch (Exception e)
            {
                response = new Response { error = e.ToString() };
            }

            callBack(response);
        }

        public void writeMultipartObject(Stream stream, object data)
        {
            using (StreamWriter writer = new StreamWriter(stream))
            {
                if (data != null)
                {
                    foreach (var entry in data as Dictionary<string, object>)
                        writeEntry(writer, entry.Key, entry.Value);
                }

                writer.Write("--");
                writer.Write(boundary);
                writer.WriteLine("--");
                writer.Flush();
            }
        }

        private void writeEntry(StreamWriter writer, string key, object value)
        {
            if (value == null)
                return;

            writer.Write("--");
            writer.WriteLine(boundary);

            if (value is byte[])
            {
                byte[] ba = value as byte[];

                writer.WriteLine(@"Content-Disposition: form-data; name=""{0}""; filename=""{1}""", key, "image.jpg");
                writer.WriteLine(@"Content-Type: application/octet-stream");
                writer.WriteLine(@"Content-Type: image / jpeg");
                writer.WriteLine(@"Content-Length: " + ba.Length);
                writer.WriteLine();
                writer.Flush();

                Stream output = writer.BaseStream;
                output.Write(ba, 0, ba.Length);
                output.Flush();
                writer.WriteLine();
            }
            else
            {
                writer.WriteLine(@"Content-Disposition: form-data; name=""{0}""", key);
                writer.WriteLine();
                writer.WriteLine(value.ToString());
            }
        }
    }

}