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

            if (WifiConfiguration.Enabled)
            {
                Thread.Sleep(5_000);

                var connectionResult = WifiNetworkHelper.ConnectDhcp(WifiConfiguration.ConnectionSSID, WifiConfiguration.ConnectionPassword);

                Debug.WriteLine($"Connection state {connectionResult}");

                if (!connectionResult)
                {
                    Thread.Sleep(5_000);
                    Power.RebootDevice();
                    return;
                }
            }

            var ws = new WebServer(80, HttpProtocol.Http);


            var methods = typeof(ConfigurationController).GetMethods();

            var routeType = typeof(RouteAttribute);
            var methodType = typeof(MethodAttribute);

            foreach (var method in methods)
            {
                var atribs = method.GetCustomAttributes(false);

                RouteInfo route = new RouteInfo()
                {
                    EndPoint = method,
                    Method = "GET"
                };

                foreach (var item in atribs)
                {
                    if (routeType.Equals(item.GetType()))
                    {
                        route.Url = ((RouteAttribute)item).Route;
                    }

                    if (methodType.Equals(item.GetType()))
                    {
                        route.Method = ((MethodAttribute)item).Method;
                    }
                }

                if (route.Url != default)
                    routes.Add(route);
            }


            ws.CommandReceived += Ws_CommandReceived;

            ws.Start();

            Debug.WriteLine("Server started");

            Thread.Sleep(Timeout.Infinite);
        }

        private static ArrayList routes = new() {
            //RouteInfo.Post("Configuration/ResetDefault", ConfigurationController.ResetOptionsContentAction),
            //RouteInfo.Post("Configuration/SaveOptions", ConfigurationController.SaveOptionsContentAction),
            //RouteInfo.Post("Configuration/SetContent", ConfigurationController.SetOptionsContentAction),
            //RouteInfo.Post("Configuration/GetContent", ConfigurationController.GetOptionsContentAction),
        };

        private static void Ws_CommandReceived(object obj, WebServerEventArgs e)
        {
            var url = e.Context.Request.RawUrl.TrimStart('/');

            foreach (var item in routes)
            {
                var route = (RouteInfo)item;

                if (url.StartsWith(route.Url))
                {
                    route.EndPoint.Invoke(null, new object[] { e });
                    return;
                }
            }

            e.Context.Response.StatusCode = 404;
        }
    }
}
