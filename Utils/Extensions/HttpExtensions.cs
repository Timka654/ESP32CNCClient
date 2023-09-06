using nanoFramework.Json;
using NFGCodeESP32Client.Configurations;
using System;
using System.Net;
using System.Text;
using nanoFramework.WebServer;

namespace NFGCodeESP32Client.Utils.Extensions
{
    public static class HttpExtensions
    {
        public static void SetBadRequest(this HttpListenerResponse response)
        {
            nanoFramework.WebServer.WebServer.OutputHttpCode(response, System.Net.HttpStatusCode.BadRequest);
        }

        public static void SetBadRequest(this HttpListenerResponse response, string content)
        {
            SetBadRequest(response);

            response.ContentType = "text/plain";

            nanoFramework.WebServer.WebServer.OutPutStream(response, content);
        }

        public static void SetOK(this HttpListenerResponse response)
        {
            nanoFramework.WebServer.WebServer.OutputHttpCode(response, System.Net.HttpStatusCode.OK);
        }

        public static void SetOK(this HttpListenerResponse response, string content)
        {
            SetOK(response);

            response.ContentType = "text/plain";

            nanoFramework.WebServer.WebServer.OutPutStream(response, content);
        }

        public static void FlushAndClose(this HttpListenerContext context)
        {
            context.Response.Close();
            context.Close();
        }

        public static object ReadBodyAsJson(this HttpListenerContext context, Type outputType)
        {
            var content = context.ReadBodyAsString();

            if (string.IsNullOrEmpty(content))
                return default;

            return JsonConvert.DeserializeObject(content, outputType);
        }

        public static string ReadBodyAsString(this HttpListenerContext context)
        {
            if (context.Request.ContentLength64 == default)
                return string.Empty;

            byte[] buf = new byte[context.Request.ContentLength64];

            context.Request.InputStream.Read(buf, 0, buf.Length);

            return Encoding.UTF8.GetString(buf, 0, buf.Length);
        }
    }
}
