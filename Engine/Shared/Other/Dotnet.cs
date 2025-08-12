using System.Xml;
using System.Diagnostics;

namespace Concrete;

public static class Dotnet
{
    public static void Execute(string parameters)
    {
        var processStartInfo = new ProcessStartInfo("dotnet", parameters)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        var process = Process.Start(processStartInfo);
        string output = process.StandardOutput.ReadToEnd();
        string errors = process.StandardError.ReadToEnd();
        process.WaitForExit();

        // if (!string.IsNullOrWhiteSpace(output)) Console.WriteLine("Output: " + output);
        if (!string.IsNullOrWhiteSpace(errors)) Console.WriteLine("Errors: " + errors);
    }

    public static void New(string projectPath)
    {
        string template = 
        @"
            <Project Sdk=""Microsoft.NET.Sdk"">

            <PropertyGroup>
                <OutputType>library</OutputType>
                <TargetFramework>net9.0</TargetFramework>
                <ImplicitUsings>enable</ImplicitUsings>
                <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
                <DebugType>embedded</DebugType>
                <SatelliteResourceLanguages>none</SatelliteResourceLanguages>
            </PropertyGroup>

            </Project>
        ";

        File.WriteAllText(projectPath, template);
    }

    public static void AddDll(string projectPath, string dllPath)
    {
        // checks
        if (!File.Exists(projectPath)) throw new FileNotFoundException($"Project file not found: {projectPath}");
        if (!File.Exists(dllPath)) throw new FileNotFoundException($"DLL file not found: {dllPath}");

        var document = new XmlDocument();
        document.Load(projectPath);

        // find or create itemgroup
        XmlNode itemGroup = document.SelectSingleNode("//ItemGroup[Reference]");
        if (itemGroup == null)
        {
            itemGroup = document.CreateElement("ItemGroup", document.DocumentElement?.NamespaceURI ?? string.Empty);
            document.DocumentElement?.AppendChild(itemGroup);
        }

        // create <Reference Include="">
        string referenceName = Path.GetFileNameWithoutExtension(dllPath);
        XmlElement reference = document.CreateElement("Reference", document.DocumentElement?.NamespaceURI ?? string.Empty);
        reference.SetAttribute("Include", referenceName);

        // create <HintPath>
        XmlElement hintPath = document.CreateElement("HintPath", document.DocumentElement?.NamespaceURI ?? string.Empty);
        string relativePath = Path.GetRelativePath(Path.GetDirectoryName(projectPath)!, dllPath);
        hintPath.InnerText = relativePath;

        reference.AppendChild(hintPath);
        itemGroup.AppendChild(reference);

        // save
        document.Save(projectPath);
        Execute($"build \"{projectPath}\"");
    }
}