using NFGCodeESP32Client.Configurations;
using System;
using System.Text;

namespace NFGCodeESP32Client.Utils.Extensions
{
    public static class ConfigurationExtensions
    {
        public static bool GetBool(this PDictionary configurations, string confKey, bool required = false, bool defaultValue = default)
        {
            if (!configurations.ContainsKey(confKey))
                if (required)
                    throw new Exception($"No have required configuration key {confKey}");
                else
                    return defaultValue;

            var value = ((string)configurations[confKey]).ToLower();

            if (value.Equals("1") || value.Equals("true"))
                return true;
            else if (value.Equals("0") || value.Equals("false"))
                return false;
            else
                throw new Exception($"Configuration value = \"{value}\" invalid for key {confKey}");
        }

        public static byte GetByte(this PDictionary configurations, string confKey, bool required = false, byte defaultValue = default)
        {
            if (!configurations.ContainsKey(confKey))
                if (required)
                    throw new Exception($"No have required configuration key {confKey}");
                else
                    return defaultValue;

            var value = (string)configurations[confKey];

            if (!byte.TryParse(value, out var pin))
                throw new Exception($"Configuration value = \"{value}\" invalid for key {confKey}");

            return pin;
        }

        public static double GetDouble(this PDictionary configurations, string confKey, bool required = false, double defaultValue = default)
        {
            if (!configurations.ContainsKey(confKey))
                if (required)
                    throw new Exception($"No have required configuration key {confKey}");
                else
                    return defaultValue;

            var value = (string)configurations[confKey];

            if (!double.TryParse(value, out var pin))
                throw new Exception($"Configuration value = \"{value}\" invalid for key {confKey}");

            return pin;
        }

        public static ushort GetUInt16(this PDictionary configurations, string confKey, bool required = false, ushort defaultValue = default)
        {
            if (!configurations.ContainsKey(confKey))
                if (required)
                    throw new Exception($"No have required configuration key {confKey}");
                else
                    return defaultValue;

            var value = (string)configurations[confKey];

            if (!ushort.TryParse(value, out var pin))
                throw new Exception($"Configuration value = \"{value}\" invalid for key {confKey}");

            return pin;
        }

        public static int GetInt(this PDictionary configurations, string confKey, bool required = false, int defaultValue = default)
        {
            if (!configurations.ContainsKey(confKey))
                if (required)
                    throw new Exception($"No have required configuration key {confKey}");
                else
                    return defaultValue;

            var value = (string)configurations[confKey];

            if (!int.TryParse(value, out var pin))
                throw new Exception($"Configuration value = \"{value}\" invalid for key {confKey}");

            return pin;
        }

        public static string GetString(this PDictionary configurations, string confKey, bool required = false, string defaultValue = default)
        {
            if (!configurations.ContainsKey(confKey))
                if (required)
                    throw new Exception($"No have required configuration key {confKey}");
                else
                    return defaultValue;

            return (string)configurations[confKey];
        }
    }
}
