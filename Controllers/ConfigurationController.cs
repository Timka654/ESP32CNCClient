using nanoFramework.Runtime.Native;
using nanoFramework.WebServer;
using NFGCodeESP32Client.Configurations;
using System;
using System.Text;

namespace NFGCodeESP32Client.Controllers
{
    public class ConfigurationController
    {
        //[Route("Configuration/GetContent")]
        //[Method("POST")]
        //public void GetOptionsContentAction(WebServerEventArgs e)
        //{
        //    //var content = DynamicConfiguration.GetContent();

        //    //e.Context.Response.ContentType = "text/plain";

        //    //e.Context.Response.StatusCode = 200;

        //    //WebServer.OutPutStream(e.Context.Response, content);
        //}

        //[Route("Configuration/SetContent")]
        //[Method("POST")]
        //public static void SetOptionsContentAction(WebServerEventArgs e)
        //{
        //    //byte[] buf = new byte[e.Context.Request.ContentLength64];

        //    //e.Context.Request.InputStream.Read(buf, 0, buf.Length);

        //    //DynamicConfiguration.LoadFromContent(Encoding.UTF8.GetString(buf, 0, buf.Length));

        //    //WebServer.OutputHttpCode(e.Context.Response, System.Net.HttpStatusCode.OK);
        //}

        //[Route("Configuration/ResetDefault")]
        //[Method("POST")]
        //public static void ResetOptionsContentAction(WebServerEventArgs e)
        //{
        //    //DynamicConfiguration.Reload();

        //    //WebServer.OutputHttpCode(e.Context.Response, System.Net.HttpStatusCode.OK);
        //}

        [Route("Configuration/SaveOptions")]
        public static void SaveOptionsContentAction(WebServerEventArgs e)
        {
            //DynamicConfiguration.SaveSettings();

            //WebServer.OutputHttpCode(e.Context.Response, System.Net.HttpStatusCode.OK);

            //e.Context.Close();

            //Power.RebootDevice();
        }
    }
}
