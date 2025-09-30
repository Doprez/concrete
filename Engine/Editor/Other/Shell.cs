using System.Diagnostics;

namespace Concrete;

public static class Shell
{
    public static void Run(string binary, string args)
    {
        var processInfo = new ProcessStartInfo
        {
            FileName = binary,
            Arguments = args,
            UseShellExecute = true,
            CreateNoWindow = true
        };

        Process.Start(processInfo);
    }

    public static bool IsCommandInPath(string command)
    {
        var paths = Environment.GetEnvironmentVariable("PATH")?.Split(Path.PathSeparator);
        if (paths == null) return false;
        string[] extensions;
        extensions = OperatingSystem.IsWindows() ? Environment.GetEnvironmentVariable("PATHEXT")?.Split(';') ?? [".exe", ".bat", ".cmd"] : [""];
        foreach (var path in paths) foreach (var ext in extensions) if (File.Exists(Path.Combine(path, command + ext))) return true;
        return false;
    }
}