using NFGCodeESP32Client.Utils;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace NFGCodeESP32Client.Configurations
{
    internal class DynamicConfiguration
    {
        private const string ConfigurationRelativeFilePath = "options.conf";

        public static PDictionary Options { get; } = new PDictionary();

        public static string GetContent()
        {
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < Options.Count; i++)
            {
                sb.AppendLine($"{Options.Keys[i]}={Options.Values[i]}");
            }

            return sb.ToString();
        }

        public static void SaveSettings()
        {
            FileSystem.WriteTextFile(ConfigurationRelativeFilePath, GetContent());
        }

        public static void Initialize()
            => Reload();

        public static void Reload()
        {
            if (!FileSystem.ExistsFile(ConfigurationRelativeFilePath))
                return;

            var content = FileSystem.ReadTextFile(ConfigurationRelativeFilePath);

            LoadFromContent(content);
        }

        public static bool LoadFromContent(string content)
        {
            Options.Clear();

            if (string.IsNullOrEmpty(content))
                return false;

            foreach (var line in content.Split('\n'))
            {
                if (string.IsNullOrEmpty(line))
                    continue;

                var keyEndIdx = line.IndexOf('=');

                if (keyEndIdx == -1)
                    continue;

                var key = line.Substring(0, keyEndIdx).Trim();

                Options[key] = line.Substring(keyEndIdx + 1).Trim();
            }

            return true;
        }
    }
}
