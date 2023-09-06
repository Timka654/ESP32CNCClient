using Iot.Device.A4988;
using nanoFramework.WebServer;
using NFGCodeESP32Client.Configurations;
using NFGCodeESP32Client.Devices;
using NFGCodeESP32Client.Utils;
using NFGCodeESP32Client.Utils.Extensions;
using System;
using System.Device.Gpio;
using System.Text;
using System.Threading;

namespace NFGCodeESP32Client.Controllers
{
    public class DrivesController
    {
        static GpioController Controller => GPIOConfiguration.Controller;

        static PDictionary Options => DynamicConfiguration.Options;

        const string SteppersEnablePinConfigurationKey = "enable_pin";

        static PDictionary stepperEnablePins = new PDictionary();

        static PDictionary steppers = new PDictionary();

        static DrivesController()
        {
            DynamicConfiguration.OnConfigurationUpdate += () =>
            {
                foreach (var item in stepperEnablePins.Values)
                {
                    ((GpioPin)item).Write(PinValue.Low);
                    ((GpioPin)item).Dispose();
                }

                stepperEnablePins.Clear();

                foreach (var item in steppers.Values)
                {
                    ((StepperMotor)item).Dispose();
                }

                steppers.Clear();
            };  
        }

        public static void DisableSteppers(string[] values)
        {
            if (stepperEnablePins.ContainsKey(string.Empty))
            {
                ((GpioPin)stepperEnablePins[string.Empty]).Write(PinValue.Low);
            }
            else if (values.Length == 0)
            {
                foreach (var item in stepperEnablePins.Values)
                {
                    ((GpioPin)item).Write(PinValue.Low);
                }
            }
            else
            {
                foreach (var item in values)
                {
                    var ilower = item.ToLower();

                    if(stepperEnablePins.ContainsKey(ilower))
                        ((GpioPin)stepperEnablePins[ilower]).Write(PinValue.Low);

                }
            }
        
        }

        #region GCodes

        public static void M84(WebServerEventArgs e)
            => M18(e);

        public static void M18(WebServerEventArgs e)
        {
            var query = e.Context.ReadBodyAsString();

            var flags = query.Split(' ');

            if (flags.TryGetIntValue('S', out var time))
            {
                var steppers = new string[flags.Length - 1];

                for (int i = 0, n = 0; i < flags.Length; i++)
                {
                    if ($"S{time}".Equals(flags[0]))
                        continue;

                    steppers[n++] = flags[i];
                }

                new Thread(() => { Thread.Sleep(time); DisableSteppers(steppers); }).Start();
            }
            else
                DisableSteppers(flags);


            e.Context.Response.SetOK();
        }

        public static void M17(WebServerEventArgs e)
        {
            if (Options.ContainsKey($"steppers_{SteppersEnablePinConfigurationKey}"))
            {
                GpioPin pin;

                if (stepperEnablePins.ContainsKey(string.Empty))
                    pin = (GpioPin)stepperEnablePins[string.Empty];
                else
                {
                    pin = Controller.OpenPin(Options.GetByte($"steppers_{SteppersEnablePinConfigurationKey}"), PinMode.Output);

                    stepperEnablePins[string.Empty] = pin;
                }

                pin.Write(PinValue.High);
            }
            else
            {
                var flags = e.Context.ReadBodyAsString();

                if (string.IsNullOrEmpty(flags))
                {
                    e.Context.Response.SetBadRequest($"Not have configuration required key \"{$"steppers_{SteppersEnablePinConfigurationKey}"}\"");

                    return;
                }

                try
                {
                    var pflags = flags.Split(' ');

                    foreach (var f in pflags)
                    {
                        if (string.IsNullOrEmpty(f))
                            continue;

                        var flower = f.ToLower();

                        GpioPin pin;

                        if (stepperEnablePins.ContainsKey(flower))
                            pin = (GpioPin)stepperEnablePins[flower];
                        else
                        {
                            pin = Controller.OpenPin(Options.GetByte($"stepper_{flower}_{SteppersEnablePinConfigurationKey}", true), PinMode.Output);

                            stepperEnablePins[flower] = pin;
                        }

                        pin.Write(PinValue.High);
                    }

                }
                catch (Exception ex)
                {
                    e.Context.Response.SetBadRequest(ex.Message);
                    return;
                }
            }

            e.Context.Response.SetOK();
        }

        public static void G0(WebServerEventArgs e)
        {
            var query = e.Context.ReadBodyAsString();

            var parameters = query.ParseGParameters();

            parameters.TryGetValue("f", out var fRate);

            foreach (var item in parameters.Keys)
            {

            }
        }

        public static void G1(WebServerEventArgs e)
            => G0(e);

        #endregion
    }
}
