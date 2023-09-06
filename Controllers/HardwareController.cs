using nanoFramework.Runtime.Native;
using nanoFramework.WebServer;
using NFGCodeESP32Client.Utils.Extensions;
using System;
using System.Text;

namespace NFGCodeESP32Client.Controllers
{
    public class HardwareController
    {
        public static void Reboot(WebServerEventArgs e)
        {
            WebServer.OutputHttpCode(e.Context.Response, System.Net.HttpStatusCode.OK);

            e.Context.FlushAndClose();

            Power.RebootDevice();
        }
    }
}
