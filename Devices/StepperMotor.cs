using Iot.Device.A4988;
using NFGCodeESP32Client.Configurations;
using NFGCodeESP32Client.Controllers;
using NFGCodeESP32Client.Utils;
using NFGCodeESP32Client.Utils.Extensions;
using System;
using System.Device.Gpio;
using System.Text;
using System.Threading;
using UnitsNet;

namespace NFGCodeESP32Client.Devices
{
    public class StepperMotor : IDisposable
    {
        private const string StepPinConfigurationName = "step_pin";
        private const string DirPinConfigurationName = "dir_pin";

        private const string DirRevertConfigurationName = "dir_revert";
        private const string MicrostepsConfigurationName = "microsteps";
        private const string RotationDistanceConfigurationName = "rotation_distance";
        private const string EndstopMinConfigurationName = "endstop_min";
        private const string EndstopMaxConfigurationName = "endstop_max";

        private const string PositionMinConfigurationName = "position_min";
        private const string PositionMaxConfigurationName = "position_max";
        private const string EndstopsIgnoreConfigurationName = "endstops_ignore";

        private readonly GpioController gpioController;

        public string ErrorMessage { get; private set; }

        public GpioPin StepPin { get; }

        public GpioPin DirPin { get; }

        public bool DirRevert { get; } = false;

        public ushort RotationDistance { get; }

        public Microsteps Microsteps { get; }

        public string Name { get; }

        public double Position { get; private set; }

        public bool NAPosition { get; private set; } = true;

        public double MinPosition { get; }

        public double MaxPosition { get; }

        public bool EndstopsIgnore { get; }

        private A4988C driver;

        public AxisEndStop MinStop { get; }

        public AxisEndStop MaxStop { get; }

        private CancellationTokenSource processCancelTokenSource = new CancellationTokenSource();

        public StepperMotor(string name, PDictionary endstopsMap, GpioController gpioController)
        {
            this.Name = name;

            this.gpioController = gpioController;

            var stepperPrefix = $"stepper_{name.ToLower()}";

            try
            {
                MinPosition = DynamicConfiguration.Options.GetDouble($"{stepperPrefix}_{PositionMinConfigurationName}", defaultValue: 0);

                MaxPosition = DynamicConfiguration.Options.GetDouble($"{stepperPrefix}_{PositionMaxConfigurationName}", true);

                StepPin = gpioController.OpenPin(
                    DynamicConfiguration.Options.GetByte($"{stepperPrefix}_{StepPinConfigurationName}", true),
                    PinMode.Output);

                DirPin = gpioController.OpenPin(
                    DynamicConfiguration.Options.GetByte($"{stepperPrefix}_{DirPinConfigurationName}", true),
                    PinMode.Output);

                DirRevert = DynamicConfiguration.Options.GetBool($"{stepperPrefix}_{DirRevertConfigurationName}");

                Microsteps = (Microsteps)DynamicConfiguration.Options.GetByte($"{stepperPrefix}_{MicrostepsConfigurationName}");

                EndstopsIgnore = DynamicConfiguration.Options.GetBool($"{stepperPrefix}_{EndstopsIgnoreConfigurationName}", defaultValue: false);

                switch (Microsteps)
                {
                    case Microsteps.FullStep:
                    case Microsteps.HalfStep:
                    case Microsteps.QuaterStep:
                    case Microsteps.EightStep:
                    case Microsteps.SisteenthStep:
                        break;
                    default:
                        throw new Exception($"Invalid microsteps = {Microsteps}");
                }

                RotationDistance = DynamicConfiguration.Options.GetUInt16($"{stepperPrefix}_{RotationDistanceConfigurationName}", defaultValue: 40);


                if (DynamicConfiguration.Options.TryGetValue($"{stepperPrefix}_{EndstopMinConfigurationName}", out var endstop_min_pin))
                {
                    if (endstopsMap.TryGetValue(endstop_min_pin, out var endstop_min))
                        MinStop = (AxisEndStop)endstop_min;
                    else
                    {
                        MinStop = new AxisEndStop((string)endstop_min_pin, gpioController);

                        if (!string.IsNullOrEmpty(MinStop.ErrorMessage))
                            throw new Exception($"Cannot init max_endstop with name {stepperPrefix}_{EndstopMaxConfigurationName} ({MinStop.ErrorMessage})");

                        endstopsMap.Add(MinStop.Name, MinStop);
                    }

                    MinStop.OnStateChanged += MinStop_OnStateChanged;
                }

                if (DynamicConfiguration.Options.TryGetValue($"{stepperPrefix}_{EndstopMaxConfigurationName}", out var endstop_max_pin))
                {
                    if (endstopsMap.TryGetValue(endstop_max_pin, out var endstop_max))
                        MaxStop = (AxisEndStop)endstop_max;
                    else
                    {
                        MaxStop = new AxisEndStop((string)endstop_max_pin, gpioController);

                        if (!string.IsNullOrEmpty(MaxStop.ErrorMessage))
                            throw new Exception($"Cannot init max_endstop with name {stepperPrefix}_{EndstopMaxConfigurationName} ({MaxStop.ErrorMessage})");

                        endstopsMap.Add(MaxStop.Name, MaxStop);
                    }

                    MaxStop.OnStateChanged += MaxStop_OnStateChanged;
                }

                driver = new A4988C(StepPin, DirPin, Microsteps, RotationDistance, TimeSpan.Zero, gpioController, false);
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }

        }

        private void MaxStop_OnStateChanged()
        {
            if (MaxStop.State)
            {
                processCancelTokenSource.Cancel();
                Position = MaxPosition;
                NAPosition = false;
            }
        }

        private void MinStop_OnStateChanged()
        {
            if (MinStop.State)
            {
                processCancelTokenSource.Cancel();
                Position = MinPosition;
                NAPosition = false;
            }
        }

        public void Home()
        {
            if (MinStop != null)
            {
                Move(-1000, true);

                ErrorMessage = default;

                return;
            }

            if (MaxStop != null)
            {
                Move(1000, true);

                ErrorMessage = default;

                Move(-Position);

                ErrorMessage = default;

                return;
            }

            if (!EndstopsIgnore)
                ErrorMessage = $"Cannot invoke home - stepper {Name} no have min/max endpoint.";
        }

        public void Move(double distance, bool ignoreCheck = false)
        {
            if (!ignoreCheck)
            {
                if (NAPosition)
                {
                    ErrorMessage = $"Cannot move {Name} axis. Need to set position or homing";
                    return;
                }

                var endPosition = DrivesController.AbsoluteCoordinates ? distance : Position + distance;

                if (endPosition > MaxPosition || endPosition < MinPosition)
                {
                    ErrorMessage = $"Cannot move {Name} to position {endPosition} {{ {nameof(MinPosition)} = {MinPosition}, {nameof(MaxPosition)} = {MaxPosition} }}";

                    return;
                }

                distance = endPosition - Position;
            }

            if (DirRevert)
                distance = -distance;

            Position += distance;

            var angle = (distance / RotationDistance) * 360;

            if (processCancelTokenSource.IsCancellationRequested)
                processCancelTokenSource = new CancellationTokenSource();

            var procesed = driver.Rotate(new Angle(angle, UnitsNet.Units.AngleUnit.Degree), processCancelTokenSource.Token);

            if (procesed != 100)
            {
                Position -= distance - (distance / 100.0 * procesed);
                ErrorMessage = $"Stepper {Name} have error on motion(processed = {procesed}%, currentPosition = {Position})";
            }
        }

        public void MoveAngle(double angle)
        {
            if (DirRevert)
                angle = -angle;

            if (processCancelTokenSource.IsCancellationRequested)
                processCancelTokenSource = new CancellationTokenSource();

            driver.Rotate(new Angle(angle, UnitsNet.Units.AngleUnit.Degree));
        }

        public void Dispose()
        {
            StepPin?.Dispose();

            DirPin?.Dispose();

            if (driver != null)
                driver.Dispose();
        }

        internal void SetPosition(double position)
        {
            NAPosition = false;
            Position = position;
        }
    }
}

namespace Iot.Device.A4988
{
    /// <summary>
    /// Class for controlling A4988 stepper motor driver.
    /// copy of <see cref="Iot.Device.A4988.A4988"/>
    /// </summary>
    public class A4988C : IDisposable
    {
        private readonly Microsteps _microsteps;

        private readonly ushort _fullStepsPerRotation;

        private readonly TimeSpan _sleepBetweenSteps;

        private readonly GpioPin _stepPin;

        private readonly GpioPin _dirPin;

        private readonly bool _shouldDispose;

        //
        // Сводка:
        //     Initializes a new instance of the Iot.Device.A4988.A4988 class.
        //
        // Параметры:
        //   stepPin:
        //     Pin connected to STEP driver pin.
        //
        //   dirPin:
        //     Pin connected to DIR driver pin.
        //
        //   microsteps:
        //     Microsteps mode.
        //
        //   fullStepsPerRotation:
        //     Full steps per rotation.
        //
        //   sleepBetweenSteps:
        //     By changing this parameter you can set delay between steps and control the rotation
        //     speed (less time equals faster rotation).
        //
        //   gpioController:
        //     GPIO controller.
        //
        //   shouldDispose:
        //     True to dispose the Gpio Controller.
        /// <summary>
        /// 
        /// </summary>
        /// <param name="stepPin"> must be configured for output and cannot be disposed previous this instance</param>
        /// <param name="dirPin"></param>
        /// <param name="microsteps"></param>
        /// <param name="fullStepsPerRotation"></param>
        /// <param name="sleepBetweenSteps"></param>
        /// <param name="gpioController"></param>
        /// <param name="shouldDispose"></param>
        public A4988C(GpioPin stepPin, GpioPin dirPin, Microsteps microsteps, ushort fullStepsPerRotation, TimeSpan sleepBetweenSteps, GpioController? gpioController = null, bool shouldDispose = true)
        {
            _microsteps = microsteps;
            _fullStepsPerRotation = fullStepsPerRotation;
            _sleepBetweenSteps = sleepBetweenSteps;

            _shouldDispose = shouldDispose || gpioController == null;
            _stepPin = stepPin;
            _dirPin = dirPin;
        }

        //
        // Сводка:
        //     Controls the speed of rotation.
        protected virtual void SleepBetweenSteps()
        {
            if (!(_sleepBetweenSteps == TimeSpan.Zero))
            {
                Thread.Sleep(_sleepBetweenSteps);
            }
        }

        //
        // Сводка:
        //     Rotates a stepper motor.
        //
        // Параметры:
        //   angle:
        //     Angle to rotate.
        public virtual double Rotate(Angle angle, CancellationToken cancellationToken = default)
        {
            if (angle.Degrees != 0.0)
            {
                _dirPin.Write((angle.Degrees > 0.0) ? PinValue.High : PinValue.Low);

                double num = ((angle.Degrees < 0.0) ? (0.0 - angle.Degrees) : angle.Degrees);

                double num2 = num / 360.0 * (double)(int)_fullStepsPerRotation * (double)(int)_microsteps;

                int i = 0;
                try
                {
                    for (; (double)i < num2; i++)
                    {
                        if (!Equals(cancellationToken, default(CancellationToken)))
                            cancellationToken.ThrowIfCancellationRequested();

                        _stepPin.Write(PinValue.High);
                        SleepBetweenSteps();

                        _stepPin.Write(PinValue.Low);
                        SleepBetweenSteps();
                    }

                }
                catch (Exception)
                {
                    return (i / num2) * 100;
                }

            }

            return 100;
        }

        public void Dispose()
        {
            if (_shouldDispose)
            {

            }
        }
    }
}