using System.Diagnostics;
using System.Numerics;

namespace Concrete;

public static class ProjectBuilder
{
    public static void StartBuilding(string build_directory)
    {
        Console.WriteLine($"Building game to: {build_directory}");

        // delete existing directory children
        var files = Directory.GetFiles(build_directory);
        for (int i = 0; i < files.Length; i++) File.Delete(files[i]);
        var dirs = Directory.GetDirectories(build_directory);
        for (int i = 0; i < dirs.Length; i++) Directory.Delete(dirs[i], true);

        // move script assembly dll to build dir
        var dllbytes = ScriptManager.RecompileScripts();
        File.WriteAllBytes(Path.Combine(build_directory, "Scripts.dll"), dllbytes);

        // move game assets to build directory
        CopyDirectory(ProjectManager.projectRoot, Path.Combine(build_directory, "Data"));

        // build player with msbuild
        BuildPlayer(build_directory);

        Console.WriteLine($"Finished building game to: {build_directory}");
    }

    public static void BuildPlayer(string build_directory)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"publish C:/Users/sjoer/Documents/GitHub/concrete/Engine/Player/Player.csproj -o {build_directory} -r win-x64 -c release",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        process.Start();
        string output = process.StandardOutput.ReadToEnd();
        string errors = process.StandardError.ReadToEnd();
        process.WaitForExit();

        Console.WriteLine(output);
        if (!string.IsNullOrWhiteSpace(errors)) Console.WriteLine("Errors: " + errors);
    }

    public static void CopyDirectory(string source, string dest)
    {
        // ensure existence
        if (!Directory.Exists(source)) throw new DirectoryNotFoundException($"directory not found: {source}");

        // create destination
        Directory.CreateDirectory(dest);

        // copy files
        foreach (string file in Directory.GetFiles(source))
        {
            string destFile = Path.Combine(dest, Path.GetFileName(file));
            File.Copy(file, destFile);
        }

        // copy dirs
        foreach (string subdir in Directory.GetDirectories(source))
        {
            string dubdirdest = Path.Combine(dest, Path.GetFileName(subdir));
            CopyDirectory(subdir, dubdirdest);
        }
    }
}