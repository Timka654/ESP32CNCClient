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

        private static void InitAllSteppers()
        {
            if (!allInited)
            {
                ArrayList inited = new ArrayList();
                foreach (var item in DynamicConfiguration.Options.Keys)
                {
                    var key = (string)item;

                    if (key.StartsWith("stepper_"))
                    {
                        key = key.Substring("stepper_".Length);

                        var eIdx = key.IndexOf('_');

                        if (eIdx > -1)
                            key = key.Substring(0, eIdx);

                        if (inited.Contains(key))
                            continue;

                        inited.Add(key);

                        GetOrInitStepper(key);
                    }
                }

                allInited = true;
            }
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

        private static string MoveArc(string query, bool clockwise)
        {
            var parameters = query.ParseGParameters();

            bool haveI = parameters.TryGetDoubleValue("i", out var i);
            bool haveJ = parameters.TryGetDoubleValue("j", out var j);

            double x = double.MinValue;
            double y = double.MinValue;

            if (parameters.TryGetDoubleValue("x", out var cx))
                x = cx;

            if (parameters.TryGetDoubleValue("y", out var cy))
                y = cy;

            if (haveI || haveJ)
            {
                MoveArcIJ(i, j, x, y, clockwise);
            }
            else if (parameters.TryGetDoubleValue("r", out var cr))
            {
                //throw new Exception($"G2/G3 code with R parameter have invalid value \"{(string)p__r}\"");

                if (x == double.MinValue && y == double.MinValue)
                    throw new Exception($"G2/G3 code with R parameter must have any X/Y parameters");

                MoveArcIJ(cr, cr, x, y, true);

            }
            else
                throw new Exception($"G2/G3 code must have parameters I/J or R, and this must be double/number, cannot invoke");

            return default;
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

        public static string M114(string ps)
        {
            InitAllSteppers();

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

            return resultContent;
        }

        public static string G28(string ps)
        {

            var keys = ps.ParseGParameters();

            var oContains = keys.ContainsKey("o");

            if ((oContains && keys.Count == 1) || keys.Count == 0)
            {
                InitAllSteppers();

                foreach (var item in steppers.Values)
                {
                    var motor = (StepperMotor)item;

                    if ((!motor.NAPosition && oContains) || !oContains)
                        motor.Home();
                }
            }
            else
            {
                foreach (var item in keys.Keys)
                {
                    if ((string)item == "o")
                        continue;

                    var motor = GetOrInitStepper((string)item);

                    if ((!motor.NAPosition && oContains) || !oContains)
                        motor.Home();
                }
            }

            return null;
        }

        public static string G90(string ps)
        {
            AbsoluteCoordinates = true;

            return default;
        }

        public static string G91(string ps)
        {
            AbsoluteCoordinates = false;

            return default;
        }

        public static string M84(string ps)
            => M18(ps);

        public static string M18(string ps)
        {
            var flags = ps.ParseGParameters();

            bool hasSleep = flags.TryGetIntValue('s', out var time);

            var steppers = new string[flags.Count - 1];

            for (int i = 0, n = 0; i < flags.Count; i++)
            {
                if (flags.Keys[i] != "s")
                    continue;

                steppers[n++] = (string)flags.Keys[i];
            }

            if (hasSleep)
                new Thread(() => { Thread.Sleep(time); DisableSteppers(steppers); }).Start();
            else
                DisableSteppers(steppers);

            return default;
        }

        public static string M17(string ps)
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
                if (string.IsNullOrEmpty(ps))
                {
                    throw new Exception($"Not have configuration required key \"{$"steppers_{SteppersEnablePinConfigurationKey}"}\"");
                }

                var pflags = ps.ParseGParameters();

                foreach (var value in pflags.Keys)
                {
                    var f = (string)value;

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

            return default;
        }

        public static string G92(string ps)
        {
            var parameters = ps.ParseGParameters();

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
                    throw new Exception($"axis {item} have invalid move value {(string)parameters[item]}");
                }
            }

            return default;
        }

        public static string G0(string ps)
        {
            var parameters = ps.ParseGParameters();

            parameters.TryGetDoubleValue("f", out var fRate);

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
                    throw new Exception($"axis {item} have invalid move value {(string)parameters[item]}");
                }
            }

            return default;
        }

        public static string G1(string ps)
            => G0(ps);

        public static string G2(string ps)
            => MoveArc(ps, true);

        public static string G3(string ps)
            => MoveArc(ps, false);

        #endregion
    }
}
