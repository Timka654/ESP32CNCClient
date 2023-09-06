using nanoFramework.Runtime.Native;
using nanoFramework.WebServer;
using NFGCodeESP32Client.Configurations;
using NFGCodeESP32Client.Utils.Extensions;
using System;
using System.Text;

namespace NFGCodeESP32Client.Controllers
{
    public class HardwareController
    {
        public static void FirmwareVersion(WebServerEventArgs e)
        {
            e.Context.Response.ContentType = "text/plain";

            WebServer.OutPutStream(e.Context.Response, FirmwareConfiguration.FirmwareVersion);
        }

        public static void Reboot(WebServerEventArgs e)
        {
            e.Context.Response.SetOK();

            e.Context.FlushAndClose();

            Power.RebootDevice(2_000);
        }

        #region GCodes

        public static void M115(WebServerEventArgs e)
            => FirmwareVersion(e);

        #endregion
    }
}
