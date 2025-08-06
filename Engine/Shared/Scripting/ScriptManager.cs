using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;

namespace Concrete;

public static class ScriptManager
{
    private static Assembly cachedAssembly;

    public static byte[] RecompileScripts()
    {
        string root = ProjectManager.projectRoot;

        // scan entire project root recursively for all script files
        var scriptPaths = Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories).ToList();

        if (scriptPaths.Count == 0)
        {
            Console.WriteLine("No scripts found to compile.");
            cachedAssembly = null;
            return null;
        }

        Console.WriteLine($"Compiling {scriptPaths.Count} script(s)...");

        var compiledAssembly = ScriptCompiler.CompileScripts(scriptPaths, out var errors, out var dllbytes);

        if (compiledAssembly == null)
        {
            Console.WriteLine($"Script compilation failed with {errors.Count} errors");
            foreach (var error in errors) Console.WriteLine(error.ToString());
            cachedAssembly = null;
            return null;
        }

        cachedAssembly = compiledAssembly;

        Console.WriteLine("Scripts compiled and loaded successfully.");

        return dllbytes;
    }

    public static void LoadCompilesScriptsFromDisk(string dllpath)
    {
        Console.WriteLine("Loading script assembly from disk");
        Assembly.LoadFile(dllpath);
        Console.WriteLine("Finished loading script assembly from disk");
    }
}