using Iot.Device.A4988;
using System;
using System.Device.Gpio;
using System.Threading;
using UnitsNet;

namespace NFGCodeESP32Client.Devices
{
    /// <summary>
    /// Class for controlling A4988 stepper motor driver.
    /// copy of <see cref="A4988"/>
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
                _dirPin.Write(angle.Degrees > 0.0 ? PinValue.High : PinValue.Low);

                double num = angle.Degrees < 0.0 ? 0.0 - angle.Degrees : angle.Degrees;

                double num2 = num / 360.0 * _fullStepsPerRotation * (int)_microsteps;

                int i = 0;
                try
                {
                    for (; i < num2; i++)
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
                    return i / num2 * 100;
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