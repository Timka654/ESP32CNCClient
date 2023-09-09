using nanoFramework.Networking;
using nanoFramework.Runtime.Native;
using nanoFramework.WebServer;
using NFGCodeESP32Client.Configurations;
using NFGCodeESP32Client.Controllers;
using NFGCodeESP32Client.Utils.Extensions;
using NFGCodeESP32Client.Utils.WebServer;
using System;
using System.Collections;
using System.Device.Gpio;
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

            if (!WifiConfiguration.InitializeWIFIConnection())
                return;

            if (!WebServerConfiguration.InitializeWebServer())
                return;

            Thread.Sleep(Timeout.Infinite);
        }
    }
}
