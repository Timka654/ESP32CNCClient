using Iot.Device.A4988;
using nanoFramework.WebServer;
using NFGCodeESP32Client.Configurations;
using NFGCodeESP32Client.Devices;
using NFGCodeESP32Client.Utils;
using NFGCodeESP32Client.Utils.Extensions;
using System;
using System.Collections;
using System.Device.Gpio;
using System.Text;
using System.Threading;

namespace NFGCodeESP32Client.Controllers
{
    public class DrivesController
    {
        static GpioController gpioController => GPIOConfiguration.Controller;

        static PDictionary Options => DynamicConfiguration.Options;

        const string SteppersEnablePinConfigurationKey = "enable_pin";

        static PDictionary stepperEnablePins = new PDictionary();

        static PDictionary steppers = new PDictionary();

        static PDictionary stepperEndStops = new PDictionary();

        public static bool AbsoluteCoordinates { get; private set; } = true;

        static DrivesController()
        {
            DynamicConfiguration.OnConfigurationUpdate += () =>
            {
                foreach (var item in stepperEnablePins.Values)
                {
                    var pin = (GpioPin)item;

                    pin.Write(PinValue.Low);

                    pin.Dispose();
                }

                stepperEnablePins.Clear();

                foreach (var item in stepperEndStops.Values)
                {
                    var endStop = (AxisEndStop)item;

                    endStop.Dispose();
                }

                stepperEndStops.Clear();

                foreach (var item in steppers.Values)
                {
                    var stepper = (StepperMotor)item;

                    stepper.Dispose();
                }

                steppers.Clear();
            };
        }

        public static void DisableSteppers(string[] steppers)
        {
            if (stepperEnablePins.ContainsKey(string.Empty))
            {
                ((GpioPin)stepperEnablePins[string.Empty]).Write(PinValue.High);
            }
            else if (steppers.Length == 0)
            {
                foreach (var item in stepperEnablePins.Values)
                {
                    ((GpioPin)item).Write(PinValue.High);
                }
            }
            else
            {
                foreach (var item in steppers)
                {
                    var ilower = item.ToLower();

                    if (stepperEnablePins.ContainsKey(ilower))
                        ((GpioPin)stepperEnablePins[ilower]).Write(PinValue.High);

                }
            }

        }

        static bool allInited = false;

        private static string InitSteppers()
        {
            if (!allInited)
            {
                foreach (var item in DynamicConfiguration.Options.Keys)
                {
                    var key = (string)item;

                    if (key.StartsWith("stepper_"))
                    {
                        key = key.Substring("stepper_".Length);

                        var eIdx = key.IndexOf('_');

                        if (eIdx > -1)
                            key = key.Substring(0, eIdx + 1);

                        var stepper = GetOrInitStepper(key);
                    }
                }

                allInited = true;
            }

            return null;
        }

        private static StepperMotor GetOrInitStepper(string key)
        {
            StepperMotor stepper = default;

            if (steppers.TryGetValue(key, out var _stepper))
                stepper = (StepperMotor)_stepper;
            else
            {
                stepper = new StepperMotor((string)key, stepperEndStops, gpioController);

                steppers[key] = stepper;
            }

            return stepper;
        }

        private static void MoveArc(WebServerEventArgs e, bool clockwise)
        {
            var query = e.Context.ReadBodyAsString();

            var parameters = query.ParseGParameters();

            object p__j = default;
            object p__i = default;

            if (parameters.TryGetValue("i", out p__i) || parameters.TryGetValue("j", out p__j))
            {
                double i = 0;
                double j = 0;

                if (p__i != default)
                    double.TryParse((string)p__i, out i);

                if (p__j != default)
                    double.TryParse((string)p__j, out j);

                double x = double.MinValue;
                double y = double.MinValue;

                if (parameters.TryGetValue("x", out var p__x) && double.TryParse((string)p__x, out var cx))
                    x = cx;

                if (parameters.TryGetValue("y", out var p__y) && double.TryParse((string)p__y, out var cy))
                    y = cy;

                MoveArcIJ(i, j, x, y, clockwise);
            }
            else if (parameters.TryGetValue("r", out var p__r))
            {
                double x = double.MinValue;
                double y = double.MinValue;

                if (!double.TryParse((string)p__r, out var cr))
                    throw new Exception($"G2/G3 code with R parameter have invalid value \"{(string)p__r}\"");

                if (parameters.TryGetValue("x", out var p__x) && double.TryParse((string)p__x, out var cx))
                    x = cx;

                if (parameters.TryGetValue("y", out var p__y) && double.TryParse((string)p__y, out var cy))
                    y = cy;

                if (x == double.MinValue && y == double.MinValue)
                    throw new Exception($"G2/G3 code with R parameter must have any X/Y parameters");

                MoveArcIJ(cr, cr, x, y, true);

            }
            else
                throw new Exception($"G2/G3 code must have parameters I/J or R, and this must be double/number, cannot invoke");
        }

        private static void MoveArcIJ(double i, double j, double x, double y, bool clockwise)
        {
            var xStepper = GetOrInitStepper("x");

            var yStepper = GetOrInitStepper("y");

            if (x == double.MinValue)
                x = xStepper.Position;

            if (y == double.MinValue)
                y = yStepper.Position;


            double centerX = xStepper.Position + i;
            double centerY = yStepper.Position + j;

            MoveArc(
                xStepper.Position, yStepper.Position,
                x, y,
                centerX, centerY,
                xStepper, yStepper,
                clockwise);
        }


        public static void MoveArc(
        double startX, double startY,
        double endX, double endY,
        double centerX, double centerY,
        StepperMotor xStepper, StepperMotor yStepper,
        bool clockwise = true, double resolution = 0.01)
        {
            // Рассчитайте радиус дуги
            double radius = Math.Sqrt(Math.Pow(centerX - startX, 2) + Math.Pow(centerY - startY, 2));

            // Определите начальный угол дуги относительно центра
            double startAngle = Math.Atan2(startY - centerY, startX - centerX);

            // Определите конечный угол дуги относительно центра
            double endAngle = Math.Atan2(endY - centerY, endX - centerX);

            double currentAngle = startAngle;


            double nextX = xStepper.Position, nextY = yStepper.Position;

            double lx = nextX;
            double ly = nextY;

            CancellationTokenSource cts = new CancellationTokenSource();

            new Thread(new ThreadStart(() =>
            {
                while (!cts.Token.IsCancellationRequested)
                {
                    if (lx != nextX)
                    {
                        xStepper.Move(nextX);
                        lx = nextX;
                    }

                    Thread.Sleep(20);
                }

            })).Start();

            new Thread(new ThreadStart(() =>
            {
                while (!cts.Token.IsCancellationRequested)
                {
                    if (ly != nextY)
                    {
                        yStepper.Move(nextY);
                        ly = nextY;
                    }

                    Thread.Sleep(20);
                }
            })).Start();


            // Для G2 или G3 добавьте точки на дуге и прямой линии
            if (clockwise)
            {
                if (startX == endX && startY == endY)
                    endAngle += Math.PI / 2;

                while (currentAngle >= -endAngle)
                {
                    nextX = centerX + radius * Math.Cos(currentAngle);
                    nextY = centerY + radius * Math.Sin(currentAngle);

                    while (lx != nextX && ly != nextY) Thread.Sleep(20);

                    // Увеличьте угол на шаг в радианах (меньше - более точно, но более медленно)
                    currentAngle -= resolution;
                }
            }
            else
            {
                while (currentAngle <= endAngle)
                {
                    nextX = centerX + radius * Math.Cos(currentAngle);
                    nextY = centerY + radius * Math.Sin(currentAngle);

                    while (lx != nextX && ly != nextY) Thread.Sleep(20);

                    // Увеличьте угол на шаг в радианах (меньше - более точно, но более медленно)
                    currentAngle += resolution;
                }
            }
            nextX = endX; nextY = endY;

            while (lx != nextX && ly != nextY) Thread.Sleep(20);

            cts.Cancel();
        }

        #region GCodes

        public static void M114(WebServerEventArgs e)
        {
            var errorMessage = InitSteppers();

            if (errorMessage != null)
                e.Context.Response.SetBadRequest(errorMessage);

            string resultContent = "";

            foreach (var item in steppers.Values)
            {
                if (!string.IsNullOrEmpty(resultContent))
                    resultContent += " ";

                var motor = (StepperMotor)item;

                if (motor.NAPosition)
                    resultContent += $"{motor.Name}:N/A";
                else
                    resultContent += $"{motor.Name}:{motor.Position}";
            }

            e.Context.Response.SetOK(resultContent);
        }

        public static void G28(WebServerEventArgs e)
        {
            var errorMessage = InitSteppers();

            if (errorMessage != null)
                e.Context.Response.SetBadRequest(errorMessage);

            foreach (var item in steppers.Values)
            {
                var motor = (StepperMotor)item;

                motor.Home();
            }

            e.Context.Response.SetOK();
        }

        public static void G90(WebServerEventArgs e)
        {
            AbsoluteCoordinates = true;

            e.Context.Response.SetOK();
        }

        public static void G91(WebServerEventArgs e)
        {
            AbsoluteCoordinates = false;

            e.Context.Response.SetOK();
        }

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
                    pin = gpioController.OpenPin(Options.GetByte($"steppers_{SteppersEnablePinConfigurationKey}"), PinMode.Output);

                    stepperEnablePins[string.Empty] = pin;
                }

                pin.Write(PinValue.Low);
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
                            pin = gpioController.OpenPin(Options.GetByte($"stepper_{flower}_{SteppersEnablePinConfigurationKey}", true), PinMode.Output);

                            stepperEnablePins[flower] = pin;
                        }

                        pin.Write(PinValue.Low);
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

        public static void G92(WebServerEventArgs e)
        {
            var query = e.Context.ReadBodyAsString();

            var parameters = query.ParseGParameters();

            StepperMotor stepper;

            foreach (var item in parameters.Keys)
            {
                var key = (string)item;

                stepper = GetOrInitStepper(key);

                if (double.TryParse((string)parameters[item], out var pos))
                {
                    stepper.SetPosition(pos);
                }
                else
                {
                    e.Context.Response.SetBadRequest($"axis {item} have invalid move value {(string)parameters[item]}");
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

            StepperMotor stepper;

            foreach (var item in parameters.Keys)
            {
                if (item.Equals("f"))
                    continue;

                var key = (string)item;

                stepper = GetOrInitStepper(key);

                if (double.TryParse((string)parameters[item], out var distance))
                {
                    stepper.Move(distance);
                }
                else
                {
                    e.Context.Response.SetBadRequest($"axis {item} have invalid move value {(string)parameters[item]}");
                    return;
                }
            }

            e.Context.Response.SetOK();
        }

        public static void G1(WebServerEventArgs e)
            => G0(e);

        public static void G2(WebServerEventArgs e)
            => MoveArc(e, true);

        public static void G3(WebServerEventArgs e)
            => MoveArc(e, false);

        #endregion
    }
}
