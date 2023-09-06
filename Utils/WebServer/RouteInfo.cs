using nanoFramework.WebServer;
using System;
using System.Reflection;
using System.Text;

namespace NFGCodeESP32Client.Utils.WebServer
{
    public delegate void RouteHandleDelegate(WebServerEventArgs context);

    public class RouteInfo
    {
        public RouteInfo(string url, string method, RouteHandleDelegate routeHandle)
        {
            if (!url.StartsWith("/"))
                url = $"/{url}";

            Url = url;
            Method = method;
            RouteHandle = routeHandle;
        }

        public static RouteInfo Post(string url, RouteHandleDelegate routeHandle)
            => new RouteInfo(url, "POST", routeHandle);

        public string Url { get; }

        public string Method { get; }

        public RouteHandleDelegate RouteHandle { get; }
    }
}
