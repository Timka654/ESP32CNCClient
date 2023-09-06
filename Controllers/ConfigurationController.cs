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

            WebServer.OutPutStream(e.Context.Response, content);
        }

        public static void SetOptions(WebServerEventArgs e)
        {
            DynamicConfiguration.LoadFromContent(e.Context.ReadBodyAsString());

            e.Context.Response.SetOK();
        }

        public static void ResetOptions(WebServerEventArgs e)
        {
            DynamicConfiguration.Reload();

            e.Context.Response.SetOK();
        }

        public static void SaveOptions(WebServerEventArgs e)
        {
            DynamicConfiguration.SaveSettings();

            e.Context.Response.SetOK();
        }
    }
}
