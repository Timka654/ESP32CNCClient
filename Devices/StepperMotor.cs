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

        public GpioPin StepPin { get; }

        public GpioPin DirPin { get; }

        public bool DirRevert { get; } = false;

        public ushort RotationDistance { get; }

        public Microsteps Microsteps { get; }

        public string Name { get; }

        public double Position { get; private set; }

        public bool NAPosition { get => naPosition && !EndstopsIgnore; private set => naPosition = value; }

        public double MinPosition { get; }

        public double MaxPosition { get; }

        public bool EndstopsIgnore { get; }

        private A4988C driver;

        public AxisEndStop MinStop { get; }

        public AxisEndStop MaxStop { get; }

        private CancellationTokenSource processCancelTokenSource = new CancellationTokenSource();

        private bool naPosition = true;

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

                        endstopsMap.Add(MaxStop.Name, MaxStop);
                    }

                    MaxStop.OnStateChanged += MaxStop_OnStateChanged;
                }

                if (MinStop == default && MaxStop == default && !EndstopsIgnore)
                {
                    throw new Exception($"must have {EndstopMinConfigurationName} or/and {EndstopMinConfigurationName} configuration or set {EndstopsIgnoreConfigurationName} to true or 1 for ignore");
                }


                driver = new A4988C(StepPin, DirPin, Microsteps, RotationDistance, TimeSpan.FromMilliseconds(20), gpioController, false);
            }
            catch (Exception ex)
            {
                throw new Exception($"Stepper {Name} have error - {ex.Message}");
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

                return;
            }

            if (MaxStop != null)
            {
                Move(1000, true);

                Move(-Position);

                return;
            }

            if (!EndstopsIgnore)
                throw new Exception($"Cannot invoke home - stepper {Name} no have min/max endpoint.");
        }

        public void Move(double distance, bool ignoreCheck = false)
        {
            if (!ignoreCheck)
            {
                if (NAPosition)
                {
                    throw new Exception($"Cannot move {Name} axis. Need to set position or homing");
                }

                var endPosition = DrivesController.AbsoluteCoordinates ? distance : Position + distance;

                if (endPosition > MaxPosition || endPosition < MinPosition)
                    throw new Exception($"Cannot move {Name} to position {endPosition} {{ {nameof(MinPosition)} = {MinPosition}, {nameof(MaxPosition)} = {MaxPosition} }}");

                distance = endPosition - Position;
            }

            if (DirRevert)
                distance = -distance;

            Position += distance;

            var angle = (distance / RotationDistance) * 360;

            if (processCancelTokenSource.IsCancellationRequested)
                processCancelTokenSource = new CancellationTokenSource();

            var processed = driver.Rotate(new Angle(angle, UnitsNet.Units.AngleUnit.Degree), processCancelTokenSource.Token);

            if (processed != 100)
            {
                Position -= distance - (distance / 100.0 * processed);
                throw new Exception($"Stepper {Name} have error on motion(processed = {processed}%, currentPosition = {Position})");
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

            if (MinStop != null)
                MinStop.OnStateChanged -= MinStop_OnStateChanged;

            if (MaxStop != null)
                MaxStop.OnStateChanged -= MinStop_OnStateChanged;
        }

        internal void SetPosition(double position)
        {
            NAPosition = false;
            Position = position;
        }
    }
}