using System;
using System.IO;
using System.Reflection;

namespace HallownestVocalizedAudioLoader;

public static class AssemblyExtensions
{
    public static byte[]? GetBytesFromResources(string fileName)
    {
        foreach (string res in Assembly.GetExecutingAssembly().GetManifestResourceNames())
        {
            if (!res.EndsWith(fileName)) continue;
            using Stream s = Assembly.GetExecutingAssembly().GetManifestResourceStream(res) ?? throw new Exception($"Failed to find resource named {fileName} in the stream");
            if (s == null) continue;
            byte[] buffer = new byte[s.Length];
            s.Read(buffer, 0, buffer.Length);
            s.Dispose();
            return buffer;
        }

        return null;
    }
    public static string GetCurrentDirectory()
    {
        return Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
    }
}