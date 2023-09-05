using nanoFramework.WebServer;
using System;
using System.Reflection;
using System.Text;

namespace NFGCodeESP32Client.Utils.WebServer
{
    public class RouteInfo
    {
        //public RouteInfo(string url, string method, MethodInfo endPoint)
        //{
        //    Url = url;
        //    Method = method;
        //    EndPoint = endPoint;
        //}

        public string Url { get; set; }

        public string Method { get; set; }

        public MethodInfo EndPoint { get; set; }
    }
}
