using nanoFramework.Networking;
using nanoFramework.Runtime.Native;
using nanoFramework.WebServer;
using NFGCodeESP32Client.Configurations;
using NFGCodeESP32Client.Controllers;
using NFGCodeESP32Client.Utils.WebServer;
using System;
using System.Collections;
using System.Device.Wifi;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;

namespace NFGCodeESP32Client
{
    public class Program
    {
        public static void Main()
        {
            DynamicConfiguration.Initialize();

            if (!InitializeWIFIConnection())
                return;

            if (!InitializeWebServer())
                return;

            Thread.Sleep(Timeout.Infinite);
        }

        private static bool InitializeWIFIConnection()
        {
            if (WifiConfiguration.Enabled)
            {
                Debug.WriteLine($"Try connect to {WifiConfiguration.ConnectionSSID}");

                var connectionResult = WifiNetworkHelper.ConnectDhcp(WifiConfiguration.ConnectionSSID, WifiConfiguration.ConnectionPassword);

                Debug.WriteLine($"Connection state {connectionResult}");

                if (!connectionResult)
                {
                    Thread.Sleep(5_000);
                    Power.RebootDevice();
                    return false;
                }
            }
            else
            {
                Debug.WriteLine($"Wifi disabled.");
            }

            return true;
        }

        private static bool InitializeWebServer()
        {
            Debug.WriteLine("Initialize web server ");
            var ws = new WebServer(80, HttpProtocol.Http);

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
            RouteInfo.Post("Hardware/Reboot", HardwareController.Reboot),
        };

        private static void Ws_CommandReceived(object obj, WebServerEventArgs e)
        {
            var url = e.Context.Request.RawUrl.TrimStart('/');

            foreach (var item in routes)
            {
                var route = (RouteInfo)item;

                if (url.StartsWith(route.Url))
                {
                    route.RouteHandle(e);
                    return;
                }
            }

            e.Context.Response.StatusCode = 404;
        }
    }
}
