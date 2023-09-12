using nanoFramework.WebServer;
using NFGCodeESP32Client.Utils.Extensions;
using NFGCodeESP32Client.Utils.WebServer;
using System;
using System.Collections;
using System.Net;
using System.Text;

namespace NFGCodeESP32Client.Controllers
{

    public class GCodeController
    {
        private const string SubStr = "GCode/";
        private const int SubStrLen = 6;

        public static GCodeCommandHandle LatestCode { get; private set; }

        private static ArrayList codes = new ArrayList()
        { 
            // GCodes
            GCodeCommandHandle.Create("M115", HardwareController.M115),
            GCodeCommandHandle.Create("M114", DrivesController.M114),
            GCodeCommandHandle.Create("M84", DrivesController.M84),
            GCodeCommandHandle.Create("M18", DrivesController.M18),
            GCodeCommandHandle.Create("M17", DrivesController.M17),
            GCodeCommandHandle.Create("G92", DrivesController.G92),
            GCodeCommandHandle.Create("G91", DrivesController.G91),
            GCodeCommandHandle.Create("G90", DrivesController.G90),
            GCodeCommandHandle.Create("G28", DrivesController.G28),
            GCodeCommandHandle.Create("G3", DrivesController.G3),
            GCodeCommandHandle.Create("G2", DrivesController.G2),
            GCodeCommandHandle.Create("G1", DrivesController.G1),
            GCodeCommandHandle.Create("G0", DrivesController.G0),
        };

        public static void InvokeGCodeRequest(WebServerEventArgs request)
        {
            var code = request.Context.Request.RawUrl;

            if (code.Length > SubStrLen)
            {
                code = code
                    .Substring(SubStrLen)
                    .ToUpper();

                GCodeCommandHandle c;

                if (code.StartsWith("G") || code.StartsWith("M"))
                    foreach (var item in codes)
                    {
                        c = (GCodeCommandHandle)item;

                        if (c.Code.Equals(code))
                            continue;

                        LatestCode = c;

                        try { request.Context.Response.SetOK(c.Handle(request.Context.ReadBodyAsString())); }
                        catch (Exception ex) { request.Context.Response.SetBadRequest(ex.Message); }

                        return;
                    }
            }
            else if(LatestCode != default)
            {
                try { request.Context.Response.SetOK(LatestCode.Handle(request.Context.ReadBodyAsString())); }
                catch (Exception ex) { request.Context.Response.SetBadRequest(ex.Message); }
            }

            request.Context.Response.SetNotFound();
        }

    public delegate string GCodeInvokeDelegate(string parameters);

    public class GCodeCommandHandle
    {
        public GCodeCommandHandle(string code, GCodeInvokeDelegate handle)
        {
            Code = code;
            Handle = handle;
        }

        public static GCodeCommandHandle Create(string code, GCodeInvokeDelegate handle)
            => new GCodeCommandHandle(code, handle);

        public string Code { get; }

        public GCodeInvokeDelegate Handle { get; }
    }
    }
}
