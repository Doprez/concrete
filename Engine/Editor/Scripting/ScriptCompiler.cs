using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Reflection;

namespace Concrete;

public static class ScriptCompiler
{
    public static Assembly CompileScripts(List<string> paths)
    {
        // parse scripts into syntax trees
        List<SyntaxTree> syntaxTrees = [];
        for (int i = 0; i < paths.Count; i++)
        {
            string path = paths[i];
            string source = File.ReadAllText(path);
            var syntaxTree = CSharpSyntaxTree.ParseText(source);
            syntaxTrees.Add(syntaxTree);
        }

        // get reference to the public api assembly
        string executableDirectory = AppContext.BaseDirectory;
        string sharedAssemblyPath = Path.Combine(executableDirectory, "Shared.dll");
        if (!File.Exists(sharedAssemblyPath))
        {
            Console.WriteLine($"Error: Shared.dll not found at '{sharedAssemblyPath}'");
            return null;
        }
        var sharedAssemblyReference = MetadataReference.CreateFromFile(sharedAssemblyPath);

        // get references to dotnet runtime
        string[] trustedPlatformAssembliesPaths = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")).Split(Path.PathSeparator);
        var dotnetRuntimeReferences = trustedPlatformAssembliesPaths.Select(path => MetadataReference.CreateFromFile(path)).ToList();
        
        // combine all references
        List<MetadataReference> references = [];
        references.Add(sharedAssemblyReference);
        references.AddRange(dotnetRuntimeReferences);

        // compile scripts
        var compilationOptions = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);
        var compilation = CSharpCompilation.Create("ScriptsAssembly", syntaxTrees, references, compilationOptions);

        // load il into memory
        using var memoryStream = new MemoryStream();
        var result = compilation.Emit(memoryStream);

        // check for compilation errors
        if (!result.Success)
        {
            var errors = result.Diagnostics.Where(diagnosis => diagnosis.Severity == DiagnosticSeverity.Error);
            foreach (var error in errors) Console.WriteLine(error.ToString());
            return null;
        }

        // load assembly from il in memory
        var assembly = Assembly.Load(memoryStream.ToArray());

        // return the assembly
        return assembly;
    }
}