using nanoFramework.WebServer;
using NFGCodeESP32Client.Controllers;
using NFGCodeESP32Client.Utils.Extensions;
using NFGCodeESP32Client.Utils.WebServer;
using System;
using System.Collections;
using System.Diagnostics;
using System.Text;

namespace NFGCodeESP32Client.Configurations
{
    internal class WebServerConfiguration
    {
        public const string WebServerPortConfigurationName = "web_server_port";

        public static bool InitializeWebServer()
        {
            int port = DynamicConfiguration.Options.GetInt(WebServerPortConfigurationName, defaultValue: 80);

            Debug.WriteLine($"Initialize web server on {port} port");

            var ws = new WebServer(port, HttpProtocol.Http);

            ws.CommandReceived += Ws_CommandReceived;

            ws.Start();

            Debug.WriteLine("Web server started.");

            return true;
        }


        private static ArrayList routes = new() {
            RouteInfo.Post("Configuration/ResetOptions", ConfigurationController.ResetOptions),
            RouteInfo.Post("Configuration/SaveOptions", ConfigurationController.SaveOptions),
            RouteInfo.Post("Configuration/SetOptions", ConfigurationController.SetOptions),
            RouteInfo.Post("Configuration/GetOptions", ConfigurationController.GetOptions),
            RouteInfo.Post("Hardware/FirmwareVersion", HardwareController.FirmwareVersion),
            RouteInfo.Post("Hardware/Reboot", HardwareController.Reboot),
            RouteInfo.Post("Network/Ping", NetworkController.Ping),

            // GCodes
            RouteInfo.Post("GCode/M115", HardwareController.M115),
            RouteInfo.Post("GCode/M114", DrivesController.M114),
            RouteInfo.Post("GCode/M84", DrivesController.M84),
            RouteInfo.Post("GCode/M18", DrivesController.M18),
            RouteInfo.Post("GCode/M17", DrivesController.M17),
            RouteInfo.Post("GCode/G92", DrivesController.G92),
            RouteInfo.Post("GCode/G91", DrivesController.G91),
            RouteInfo.Post("GCode/G90", DrivesController.G90),
            RouteInfo.Post("GCode/G28", DrivesController.G28),
            RouteInfo.Post("GCode/G3", DrivesController.G3),
            RouteInfo.Post("GCode/G2", DrivesController.G2),
            RouteInfo.Post("GCode/G1", DrivesController.G1),
            RouteInfo.Post("GCode/G0", DrivesController.G0),

        };

        private static void Ws_CommandReceived(object obj, WebServerEventArgs e)
        {
            RouteInfo route;

            foreach (var item in routes)
            {
                route = (RouteInfo)item;

                if (!e.Context.Request.RawUrl.StartsWith(route.Url))
                    continue;

                try { route.RouteHandle(e); } catch (Exception ex) { e.Context.Response.SetBadRequest(ex.Message); }
                return;
            }

            e.Context.Response.StatusCode = 404;
        }
    }
}
