using NFGCodeESP32Client.Configurations;
using NFGCodeESP32Client.Utils;
using NFGCodeESP32Client.Utils.Configuration;
using NFGCodeESP32Client.Utils.Extensions;
using System;
using System.Device.Gpio;
using System.Diagnostics;
using System.Text;
using System.Threading;

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

                    var pType = DynamicConfiguration.Options
                        .GetString($"{axisPrefix}_{PullTypeConfigurationName}", defaultValue: nameof(InputTypeEnum.PullDown));

                    PullType = pType
                        .ParseInputType();

                    StateRevert = DynamicConfiguration.Options.GetBool($"{axisPrefix}_{RevertStateConfigurationName}", defaultValue: false);

                    var pin = DynamicConfiguration.Options.GetByte($"{axisPrefix}_{PinConfigurationName}", true);

                    if (!gpioController.IsPinModeSupported(pin, (PinMode)PullType))
                        throw new Exception($"{pType} unsupported for pin {pin}");

                    StopPin = gpioController.OpenPin(pin,
                        (PinMode)PullType);
                }
                else if (int.TryParse(name, out var pin))
                {
                    StopPin = gpioController.OpenPin(
                        pin,
                        (PinMode)InputTypeEnum.PullDown);
                }
                else
                    throw new Exception($"Invalid value for endstop = {name}");

                StopPin.ValueChanged += StopPin_ValueChanged;
                StopPin_ValueChanged(default, default);
            }
            catch (Exception ex)
            {
                throw new Exception($"{nameof(AxisEndStop)} {Name} have initial error - {ex.Message}");
            }
        }

        AutoResetEvent processLocker = new AutoResetEvent(true);

        private void StopPin_ValueChanged(object sender, PinValueChangedEventArgs e)
        {
            if (!processLocker.WaitOne(Timeout.Infinite, true))
                return;

            Logger.WriteLine($"{nameof(StopPin_ValueChanged)} invoked for {Name}");

            var state = StopPin.Read() == PinValue.High;

            if (StateRevert)
                state = !state;

            if (State != state)
            {
                Logger.WriteLine($"{Name} endstop have new state = {state}");
                State = state;
                OnStateChanged();
            }

            processLocker.Set();
        }

        public void Dispose()
        {
            StopPin.Dispose();
        }
    }
}
