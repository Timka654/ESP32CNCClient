using System;
using System.Text;

namespace NFGCodeESP32Client.Utils.Extensions
{
    public static class PDictionaryExtensions
    {
        public static bool TryGetStringValue(this PDictionary map, object key, out string value)
        {
            value = default;

            if (!map.TryGetValue(key, out var val))
                return false;

            if (val is string)
            {
                value = (string)val;

                return true;
            }

            value = val.ToString();

            return true;
        }

        public static bool TryGetIntValue(this PDictionary map, object key, out int value)
        {
            value = default;

            if (!map.TryGetValue(key, out var val))
                return false;

            if (val is int)
            {
                value = (int)val;
                return true;
            }

            if (val is string)
            {
                if (!int.TryParse((string)val, out value))
                    throw new Exception($"Cannot convert {val} to int");

                return true;
            }

            throw new Exception($"Cannot read by key {key} as int type - value {val} cannot be converted!!");
        }

        public static bool TryGetDoubleValue(this PDictionary map, object key, out double value)
        {
            value = default;

            if (!map.TryGetValue(key, out var val))
                return false;

            if (val is double)
            {
                value = (double)val;
                return true;
            }

            if (val is string)
            {
                if (!double.TryParse((string)val, out value))
                    throw new Exception($"Cannot convert {val} to double");

                return true;
            }

            throw new Exception($"Cannot read by key {key} as double type - value {val} cannot be converted!!");
        }

        public static bool TryGetBoolValue(this PDictionary map, object key, out bool value)
        {
            value = default;

            if (!map.TryGetValue(key, out var val))
                return false;

            if (val is bool)
            {
                value = (bool)val;
                return true;
            }

            if (val is string)
            {
                if (val.Equals("1") || val.Equals("true"))
                {
                    value = true;
                    return true;
                }
                else if (val.Equals("0") || val.Equals("false"))
                {
                    value = true;
                    return true;
                }

                throw new Exception($"Cannot convert {val} to bool");
            }

            throw new Exception($"Cannot read by key {key} as bool type - value {val} cannot be converted!!");
        }
    }
}
