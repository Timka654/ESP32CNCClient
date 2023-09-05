using System;
using System.IO;
using System.Text;

namespace NFGCodeESP32Client.Configurations
{
    internal class FSConfiguration
    {
        public const string InternalFileSystemLogicalDrive = "I:\\";

        // for implementation another drive(SD, EEPROM, etc...)
        public static string GetFileSystemLogicalDrive()
            => InternalFileSystemLogicalDrive;
    }
}
