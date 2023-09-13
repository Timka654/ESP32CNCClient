using System;
using System.Text;

namespace NFGCodeESP32Client.Utils.Configuration
{
    public enum InputTypeEnum
    {
        None = 0,
        PullDown = 1,
        PullUp
    }

    public static class InputTypeExtensions
    {
        public static InputTypeEnum ParseInputType(this string name)
        {
            switch (name.Trim().ToLower())
            {
                case "pullup": return InputTypeEnum.PullUp;
                case "pulldown": return InputTypeEnum.PullDown;
                case "none": return InputTypeEnum.None;
                default: throw new Exception($"{name} cannot parsed to {nameof(InputTypeEnum)}");
            }

            return InputTypeEnum.None;
        }
    }
}
