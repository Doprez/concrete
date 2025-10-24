using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;

namespace Concrete;

public static class ScriptManager
{
    public static Assembly cachedAssembly;

    public static byte[] RecompileScripts(string directoryToScan)
    {
        bool NotOfBinOrObj(string file)
        {
            var dir = Path.GetDirectoryName(file);
            var parts = dir.Split(Path.DirectorySeparatorChar);
            foreach (var part in parts) if (part == "bin" || part == "obj") return false;
            return true;
        }

        // scan entire project root recursively for all script files (excluding dotnet temps from bin and obj)
        var scriptPaths = Directory.GetFiles(directoryToScan, "*.cs", SearchOption.AllDirectories).Where(NotOfBinOrObj).ToList();

        if (scriptPaths.Count == 0)
        {
            Debug.Log("No scripts found to compile.");
            cachedAssembly = null;
            return null;
        }

        Debug.Log($"Compiling {scriptPaths.Count} script(s)...");

        var compiledAssembly = ScriptCompiler.CompileScripts(scriptPaths, out var errors, out var dllbytes);

        if (compiledAssembly == null)
        {
            Debug.Log($"Script compilation failed with {errors.Count} errors");
            foreach (var error in errors) Debug.Log(error.ToString());
            cachedAssembly = null;
            return null;
        }

        cachedAssembly = compiledAssembly;

        Debug.Log("Scripts compiled and loaded successfully.");

        return dllbytes;
    }

    public static Type GetClassTypeOfScript(string scriptPath)
    {
        string source = File.ReadAllText(scriptPath);
        var regexMatch = Regex.Match(source, @"class\s+([A-Za-z0-9_]+)");
        string className = regexMatch.Groups[1].Value;

        if (cachedAssembly != null)
        {
            var non_recomp_type = cachedAssembly.GetTypes().FirstOrDefault(x => x.Name == className);

            // script assembly already had the compiled script
            if (non_recomp_type != null) return non_recomp_type;
            else
            {
                // script assembly existed but did not contain the script yet
                RecompileScripts(ProjectManager.projectRoot);
                var recomp_type = cachedAssembly.GetTypes().FirstOrDefault(x => x.Name == className);
                return recomp_type;
            }
        }
        else
        {
            // assembly was null, needed to recompile anyway
            RecompileScripts(ProjectManager.projectRoot);
            var recomp_type = cachedAssembly.GetTypes().FirstOrDefault(x => x.Name == className);
            return recomp_type;
        }
    }
}