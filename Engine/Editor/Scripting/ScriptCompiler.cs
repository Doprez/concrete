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

        // gets references to all dll's this assembly has access to (aka, the shared dll)
        var references = AppDomain.CurrentDomain.GetAssemblies().Where(a => !a.IsDynamic).Select(a => MetadataReference.CreateFromFile(a.Location));

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