using Iot.Device.A4988;
using NFGCodeESP32Client.Configurations;
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

        private readonly GpioController gpioController;

        public string ErrorMessage { get; private set; }

        public GpioPin StepPin { get; }

        public GpioPin DirPin { get; }

        public bool DirRevert { get; } = false;

        public ushort RotationDistance { get; set; }

        public Microsteps Microsteps { get; }

        private A4988C driver;

        public StepperMotor(string stepperPrefix, GpioController gpioController)
        {
            stepperPrefix = $"stepper_{stepperPrefix.ToLower()}";

            this.gpioController = gpioController;

            try
            {

                StepPin = gpioController.OpenPin(
                    DynamicConfiguration.Options.GetByte($"{stepperPrefix}_{StepPinConfigurationName}", true),
                    PinMode.Output);

                DirPin = gpioController.OpenPin(
                    DynamicConfiguration.Options.GetByte($"{stepperPrefix}_{DirPinConfigurationName}", true),
                    PinMode.Output);

                DirRevert = DynamicConfiguration.Options.GetBool($"{stepperPrefix}_{DirRevertConfigurationName}");

                Microsteps = (Microsteps)DynamicConfiguration.Options.GetByte($"{stepperPrefix}_{MicrostepsConfigurationName}");

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

                driver = new A4988C(StepPin, DirPin, Microsteps, RotationDistance, TimeSpan.Zero, gpioController, false);
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }

        }

        public void Move(double distance)
        {
            if (DirRevert)
                distance = -distance;

            var angle = (distance / RotationDistance) * 360;

            MoveAngle(angle);
        }

        public void MoveAngle(double angle)
        {
            if (DirRevert)
                angle = -angle;
            driver.Rotate(new Angle(angle, UnitsNet.Units.AngleUnit.Degree));
        }

        public void Dispose()
        {
            StepPin?.Dispose();

            DirPin?.Dispose();

            if (driver != null)
                driver.Dispose();
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

        private GpioController _gpioController;

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
            _gpioController = gpioController ?? new GpioController();
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
        public virtual void Rotate(Angle angle)
        {
            if (angle.Degrees != 0.0)
            {
                _dirPin.Write((angle.Degrees > 0.0) ? PinValue.High : PinValue.Low);
                double num = ((angle.Degrees < 0.0) ? (0.0 - angle.Degrees) : angle.Degrees);
                double num2 = num / 360.0 * (double)(int)_fullStepsPerRotation * (double)(int)_microsteps;
                for (int i = 0; (double)i < num2; i++)
                {
                    _stepPin.Write(PinValue.High);
                    SleepBetweenSteps();
                    _stepPin.Write(PinValue.Low);
                    SleepBetweenSteps();
                }
            }
        }

        public void Dispose()
        {
            if (_shouldDispose)
            {
                _gpioController?.Dispose();
                _gpioController = null;
            }
        }
    }
}