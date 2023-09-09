using nanoFramework.Networking;
using nanoFramework.Runtime.Native;
using NFGCodeESP32Client.Utils.Extensions;
using System;
using System.Diagnostics;
using System.Text;
using System.Threading;

namespace NFGCodeESP32Client.Configurations
{
    public class WifiConfiguration
    {
        public const bool Enabled = true;

        public const string ConnectionSSID = "";

        public const string ConnectionPassword = "";

        public const string WirelessSSIDConfigurationName = "wireless_ssid";
        public const string WirelessPasswordConfigurationName = "wireless_password";

        public static bool InitializeWIFIConnection()
        {
            if (Enabled)
            {
                var connectionSSID = DynamicConfiguration.Options.GetString(WirelessSSIDConfigurationName, defaultValue: ConnectionSSID);

                var connectionPassword = DynamicConfiguration.Options.GetString(WirelessPasswordConfigurationName, defaultValue: ConnectionPassword);

                Debug.WriteLine($"Try connect to {connectionSSID}");

                var connectionResult = WifiNetworkHelper.ConnectDhcp(connectionSSID, connectionPassword);

                Debug.WriteLine($"Connection state {connectionResult}");

                if (!connectionResult)
                {
                    Thread.Sleep(5_000);
                    Power.RebootDevice(2_000);
                    return false;
                }
            }
            else
            {
                Debug.WriteLine($"Wifi disabled.");
            }

            return true;
        }
    }
}
