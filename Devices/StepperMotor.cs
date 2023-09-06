using Iot.Device.A4988;
using NFGCodeESP32Client.Configurations;
using NFGCodeESP32Client.Utils.Extensions;
using System;
using System.Device.Gpio;
using System.Text;
using UnitsNet;

namespace NFGCodeESP32Client.Devices
{
    public class StepperMotor : IDisposable
    {
        private const string StepPinConfigurationName = "step_pin";
        private const string DirPinConfigurationName = "dir_pin";

        private const string DirRevertConfigurationName = "dir_revert";
        private const string MicrostepsConfigurationName = "microsteps";

        private readonly GpioController gpioController;

        public string ErrorMessage { get; private set; }

        public GpioPin StepPin { get; }

        public GpioPin DirPin { get; }

        public bool DirRevert { get; } = false;

        public Microsteps Microsteps { get; }

        private A4988 driver;

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

                driver = new A4988((byte)StepPin.PinNumber, (byte)DirPin.PinNumber, Microsteps, 40, TimeSpan.Zero, gpioController, false);
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

            driver.Rotate(Angle.FromDegrees(distance));
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
