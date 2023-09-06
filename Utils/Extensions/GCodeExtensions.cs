using System;
using System.Text;

namespace NFGCodeESP32Client.Utils.Extensions
{
    public static class GCodeExtensions
    {
        public static PDictionary ParseGParameters(this string value)
        {
            PDictionary result = new PDictionary();

            var values = value.Split(' ');

            foreach (var item in values)
            {
                if (string.IsNullOrEmpty(item))
                    continue;

                var key = item[0].ToString();

                if (item.Equals(key))
                    result[key.ToLower()] = default;
                else
                    result[key.ToLower()] = item.Substring(1);
            }

            return result;
        }

        public static bool TryToInt(this string value, out int result)
        {
            return int.TryParse(value, out result);
        }

        public static bool TryToDouble(this string value, out double result)
        {
            return double.TryParse(value, out result);
        }

        public static bool TryGetIntValue(this string[] values, char key, out int result)
        {
            foreach (var item in values)
            {
                if (TryGetIntValue(item, key, out result))
                    return true;
            }
            
            result = default;

            return false;
        }

        public static bool TryGetIntValue(this string value, char key, out int result)
        {
            value = value.Trim();

            if (value.StartsWith(key.ToString()))
            {
                if (value.Substring(1).TryToInt(out result))
                    return true;
            }

            result = default;

            return false;
        }

        public static bool TryGetDoubleValue(this string[] values, char key, out double result)
        {
            foreach (var item in values)
            {
                if (TryGetDoubleValue(item, key, out result))
                    return true;
            }

            result = default;

            return false;
        }

        public static bool TryGetDoubleValue(this string value, char key, out double result)
        {
            value = value.Trim();

            if (value.StartsWith(key.ToString()))
            {
                if (value.Substring(1).TryToDouble(out result))
                    return true;
            }

            result = default;

            return false;
        }
    }
}
