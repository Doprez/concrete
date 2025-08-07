using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using Hexa.NET.ImGui;
using Hexa.NET.ImPlot;

namespace Concrete;

public static class BuildWindow
{
    public static string buildDirectory = "";
    private static bool building = false;
    private static bool choosingDirectory = false;
    private static string status = "Status: Choose settings for the build.";

    private static int platform = 0;
    private static string[] availablePlatforms = ["Windows x64", "Linux x64"];


    public static void Draw(float deltaTime)
    {
        ImGui.Begin("Build", ImGuiWindowFlags.NoFocusOnAppearing | ImGuiWindowFlags.NoScrollbar);

        ImGui.Text(status);

        ImGui.Separator();

        ImGui.BeginDisabled(building);

        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
        ImGui.InputText("##directory", ref buildDirectory, 800);

        if (ImGui.Button("Choose Directory")) choosingDirectory = true;

        ImGui.SameLine();

        ImGui.BeginDisabled(!Directory.Exists(buildDirectory));
        if (ImGui.Button("Start Building")) StartBuildingAsync();
        ImGui.EndDisabled();

        ImGui.Text("Choose Platform:");

        if (ImGui.BeginCombo("##platform", availablePlatforms[platform]))
        {
            for (int i = 0; i < availablePlatforms.Length; i++)
            {
                if (ImGui.Selectable(availablePlatforms[i], platform == i)) platform = i;
                if (platform == i) ImGui.SetItemDefaultFocus();
            }
            ImGui.EndCombo();
        }

        ImGui.EndDisabled();

        ImGui.End();

        if (choosingDirectory) FileDialog.Show(ref choosingDirectory, ref buildDirectory, false);
    }
    
    public async static void StartBuildingAsync() => await Task.Run(StartBuilding);

    public static void StartBuilding()
    {
        // initialize
        building = true;
        status = "Status: Game is being build (initializing...)";

        // delete existing directory children
        var files = Directory.GetFiles(buildDirectory);
        for (int i = 0; i < files.Length; i++) File.Delete(files[i]);
        var dirs = Directory.GetDirectories(buildDirectory);
        for (int i = 0; i < dirs.Length; i++) Directory.Delete(dirs[i], true);

        // move script assembly dll to build dir
        status = "Status: Game is being build (compiling scripts...)";
        var dllbytes = ScriptManager.RecompileScripts(ProjectManager.projectRoot);
        File.WriteAllBytes(Path.Combine(buildDirectory, "Scripts.dll"), dllbytes);

        // move game assets to build directory
        CopyDirectory(ProjectManager.projectRoot, Path.Combine(buildDirectory, "Data"));

        // copy player pre build files
        if (platform == 0) BuildPlayer();
        if (platform == 1) BuildPlayer();

        // finalize
        building = false;
        status = "Status: Game done building.";
        OpenDirectoryInFileExplorer(buildDirectory);
    }

    public static void BuildPlayer()
    {
        string csproj = Path.GetFullPath("Resources/SourceForGameBuilding/Player/Player.csproj");

        string rid = "";
        if (platform == 0) rid = "win-x64";
        if (platform == 1) rid = "linux-x64";

        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"publish {csproj} -o {buildDirectory} -r {rid} -c release",
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

    public static void OpenDirectoryInFileExplorer(string path)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) Process.Start("explorer.exe", path);
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) Process.Start("open", path);
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) Process.Start("xdg-open", path);
    }
}