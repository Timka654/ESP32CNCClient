using System;
using System.Net;
using System.Text;

namespace NFGCodeESP32Client.Utils.Extensions
{
    public static class HttpExtensions
    {
        public static void FlushAndClose(this HttpListenerContext context)
        {
            context.Response.Close();
            context.Close();
        }
    }
}
