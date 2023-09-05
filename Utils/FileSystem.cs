using NFGCodeESP32Client.Configurations;
using System;
using System.IO;
using System.Text;

namespace NFGCodeESP32Client.Utils
{
    internal class FileSystem
    {
        private static bool HasDir(string relativePath)
            => relativePath.LastIndexOf('\\') > -1;

        public static string GetDirectoryPath(string filePath)
        {
            var lastIdx = filePath.LastIndexOf('\\');

            if (lastIdx > -1)
                return filePath.Substring(0, lastIdx + 1);

            return filePath;
        }

        public static string GetFullPath(string relativePath)
            => Path.Combine(FSConfiguration.GetFileSystemLogicalDrive(), relativePath);

        public static bool ExistsDirectory(string relativePath)
            => Directory.Exists(GetFullPath(relativePath));

        public static void CreateDirectory(string relativePath)
        {
            if (ExistsDirectory(relativePath))
                return;

            var path = GetFullPath(relativePath);

            Directory.CreateDirectory(path);
        }

        public static bool ExistsFile(string relativePath)
            => File.Exists(GetFullPath(relativePath));

        public static void WriteTextFile(string relativePath, string content)
            => WriteBinaryFile(relativePath, Encoding.UTF8.GetBytes(content));

        public static void WriteBinaryFile(string relativePath, byte[] content)
        {
            if (HasDir(relativePath))
                CreateDirectory(GetDirectoryPath(relativePath));

            var fPath = GetFullPath(relativePath);

            using var stream = File.Create(fPath);

            stream.Write(content, 0, content.Length);
        }

        public static string ReadTextFile(string relativePath)
        {
            var buf = ReadBinaryFile(relativePath);

            return Encoding.UTF8.GetString(buf, 0, buf.Length);
        }

        public static byte[] ReadBinaryFile(string relativePath)
        {
            if (!ExistsFile(relativePath))
                throw new Exception($"File path {relativePath} no exists");

            var fPath = GetFullPath(relativePath);

            using var fs = new FileStream(fPath, FileMode.Open);

            byte[] buffer = new byte[fs.Length];

            fs.Read(buffer, 0, buffer.Length);

            return buffer;
        }
    }
}
