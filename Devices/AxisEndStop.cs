using NFGCodeESP32Client.Configurations;
using NFGCodeESP32Client.Utils;
using NFGCodeESP32Client.Utils.Configuration;
using NFGCodeESP32Client.Utils.Extensions;
using System;
using System.Device.Gpio;
using System.Diagnostics;
using System.Text;

namespace NFGCodeESP32Client.Devices
{
    public class AxisEndStop : IDisposable
    {
        private const string PinConfigurationName = "pin";
        private const string RevertStateConfigurationName = "revert_state";
        private const string PullTypeConfigurationName = "pull_type";

        public string Name { get; }

        public bool StateRevert { get; }

        public GpioPin StopPin { get; }

        public bool State { get; set; }

        public InputTypeEnum PullType { get; set; }

        public event Action OnStateChanged = () => { };

        public AxisEndStop(string name, GpioController gpioController)
        {
            Name = name;

            try
            {
                if (name.TryParseVariable(out name))
                {
                    Name = name;

                    var axisPrefix = $"endstop_{name.ToLower()}";

                    PullType = DynamicConfiguration.Options
                        .GetString($"{axisPrefix}_{PullTypeConfigurationName}", defaultValue: nameof(InputTypeEnum.PullDown))
                        .ParseInputType();

                    StopPin = gpioController.OpenPin(
                        DynamicConfiguration.Options.GetByte($"{axisPrefix}_{PinConfigurationName}", true),
                        (PinMode)PullType);

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
                throw new Exception($"{nameof(AxisEndStop)} {Name} have initial error - {ex.Message}");
            }
        }

        private void StopPin_ValueChanged(object sender, PinValueChangedEventArgs e)
        {
            Debug.WriteLine($"{nameof(StopPin_ValueChanged)} invoked");

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
