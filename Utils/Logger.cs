using System;
using System.Diagnostics;
using System.Text;

namespace NFGCodeESP32Client.Utils
{
    internal class Logger
    {
        public static void WriteLine(string line)
        {
#if DEBUG
            Debug.WriteLine(line);
#endif
        }
    }
}
