using nanoFramework.Runtime.Native;
using nanoFramework.WebServer;
using NFGCodeESP32Client.Configurations;
using NFGCodeESP32Client.Utils.Extensions;
using System;
using System.Text;

namespace NFGCodeESP32Client.Controllers
{
    public class ConfigurationController
    {
        public static void GetOptions(WebServerEventArgs e)
        {
            var content = DynamicConfiguration.GetContent();

            e.Context.Response.ContentType = "text/plain";

            e.Context.Response.StatusCode = 200;

            WebServer.OutPutStream(e.Context.Response, content);
        }

        public static void SetOptions(WebServerEventArgs e)
        {
            byte[] buf = new byte[e.Context.Request.ContentLength64];

            e.Context.Request.InputStream.Read(buf, 0, buf.Length);

            DynamicConfiguration.LoadFromContent(Encoding.UTF8.GetString(buf, 0, buf.Length));

            WebServer.OutputHttpCode(e.Context.Response, System.Net.HttpStatusCode.OK);
        }

        public static void ResetOptions(WebServerEventArgs e)
        {
            DynamicConfiguration.Reload();

            WebServer.OutputHttpCode(e.Context.Response, System.Net.HttpStatusCode.OK);
        }

        public static void SaveOptions(WebServerEventArgs e)
        {
            DynamicConfiguration.SaveSettings();

            WebServer.OutputHttpCode(e.Context.Response, System.Net.HttpStatusCode.OK);

            e.Context.FlushAndClose();

            Power.RebootDevice();
        }
    }
}
