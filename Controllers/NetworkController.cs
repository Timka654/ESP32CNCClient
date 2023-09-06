using nanoFramework.WebServer;
using NFGCodeESP32Client.Utils.Extensions;
using System;
using System.Text;

namespace NFGCodeESP32Client.Controllers
{
    public class NetworkController
    {
        public static void Ping(WebServerEventArgs e)
        {
            e.Context.Response.SetOK();
        }
    }
}
