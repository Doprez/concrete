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

    public static void RecompileScripts()
    {
        string root = ProjectManager.projectRoot;

        // scan entire project root recursively for all script files
        var scriptPaths = Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories).ToList();

        if (scriptPaths.Count == 0)
        {
            Console.WriteLine("No scripts found to compile.");
            cachedAssembly = null;
            return;
        }

        Console.WriteLine($"Compiling {scriptPaths.Count} script(s)...");

        List<Diagnostic> errors = [];
        var compiledAssembly = ScriptCompiler.CompileScripts(scriptPaths, ref errors);
        if (compiledAssembly == null)
        {
            Console.WriteLine($"Script compilation failed with {errors.Count} errors");
            foreach (var error in errors) Console.WriteLine(error.ToString());
            cachedAssembly = null;
            return;
        }

        cachedAssembly = compiledAssembly;

        Console.WriteLine("Scripts compiled and loaded successfully.");
    }

    public static Assembly GetCompiledAssembly()
    {
        return cachedAssembly;
    }
}