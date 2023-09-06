using System;
using System.Device.Gpio;
using System.Text;

namespace NFGCodeESP32Client.Configurations
{
    internal class GPIOConfiguration
    {
        public static GpioController Controller { get; } = new GpioController();
    }
}
