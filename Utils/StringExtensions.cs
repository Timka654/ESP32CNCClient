using System;
using System.Text;

namespace NFGCodeESP32Client.Utils
{
    public static class StringExtensions
    {
        public static bool TryParseVariable(this string s, out string result)
        {
            if (s.StartsWith("{") && s.EndsWith("}"))
            {
                result = s.Substring(1, s.Length - 2);
                return true;
            }

            result = s;
            return false;
        }

        public static string ClearVariable(this string s)
            => s.Trim('{', '}').Trim();
    }
}
