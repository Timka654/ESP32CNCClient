using NFGCodeESP32Client.Configurations;
using NFGCodeESP32Client.Utils.Extensions;
using System;
using System.Device.Gpio;
using System.Text;

namespace NFGCodeESP32Client.Devices
{
    public class AxisEndStop : IDisposable
    {
        private const string PinConfigurationName = "pin";
        private const string RevertStateConfigurationName = "revert_state";

        public string ErrorMessage { get; private set; }

        public string Name { get; }

        public bool StateRevert { get; }

        public GpioPin StopPin { get; }

        public bool State { get; set; }

        public event Action OnStateChanged = () => { };

        public AxisEndStop(string name, GpioController gpioController)
        {
            Name = name;

            try
            {
                if (name.StartsWith("{") && name.EndsWith("}"))
                {
                    Name = name.Substring(1, name.Length - 2);

                    var axisPrefix = $"endstop_{name.ToLower()}";

                    StopPin = gpioController.OpenPin(
                        DynamicConfiguration.Options.GetByte($"{axisPrefix}_{PinConfigurationName}", true),
                        PinMode.Input);

                    StateRevert = DynamicConfiguration.Options.GetBool($"{axisPrefix}_{RevertStateConfigurationName}", defaultValue: false);
                }
                else if (int.TryParse(name, out var pin))
                {
                    StopPin = gpioController.OpenPin(
                        pin,
                        PinMode.Input);
                }
                else
                    throw new Exception($"Invalid value for endstop = {name}");

                StopPin.ValueChanged += StopPin_ValueChanged;
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.ToString();
            }
        }

        private void StopPin_ValueChanged(object sender, PinValueChangedEventArgs e)
        {
            var state = StopPin.Read() == PinValue.High;

            if (StateRevert)
                state = !state;

            if (State != state)
            {
                State = state;
                OnStateChanged();
            }
        }

        public void Dispose()
        {
            StopPin.Dispose();
        }
    }
}
