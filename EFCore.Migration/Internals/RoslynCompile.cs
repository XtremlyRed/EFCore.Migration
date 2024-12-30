using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace EFCore.Migration.Internals;

internal static class RoslynCompile
{
    public static byte[] Compile(string scriptCode)
    {
        if (string.IsNullOrWhiteSpace(scriptCode))
        {
            throw new InvalidOperationException("scripts is null or empty");
        }

        CSharpCompilationOptions options = new(OutputKind.DynamicallyLinkedLibrary);

        System.Reflection.Assembly[] assemblies = AppDomain
            .CurrentDomain.GetAssemblies()
            .Where(i => i.IsDynamic == false)
            .Where(i => string.IsNullOrEmpty(i.Location) == false)
            .ToArray();

        PortableExecutableReference[] metadataReferences = assemblies
            .Select(i => MetadataReference.CreateFromFile(i.Location))
            .ToArray();

        CSharpCompilation compilation = CSharpCompilation
            .Create($"auto.migration.script.{Environment.TickCount}")
            .WithOptions(options)
            .AddReferences(metadataReferences)
            .AddSyntaxTrees(CSharpSyntaxTree.ParseText(scriptCode));

        using MemoryStream stream = new();

        Microsoft.CodeAnalysis.Emit.EmitResult result = compilation.Emit(stream);
        if (result.Success)
        {
            stream.Seek(0, SeekOrigin.Begin);
            return stream.ToArray();
        }

        string message = string.Join(
            Environment.NewLine,
            result.Diagnostics.Select(i => i.ToString())
        );

        throw new CompileException(result.Diagnostics, message);
    }
}

/// <summary>
///
/// </summary>
public class CompileException : Exception
{
    /// <summary>
    ///
    /// </summary>
    /// <param name="diagnostics"></param>
    /// <param name="message"></param>
    public CompileException(IReadOnlyList<object> diagnostics, string message)
        : base(message)
    {
        Diagnostics = diagnostics;
    }

    /// <summary>
    /// diagnostics
    /// </summary>
    public IReadOnlyList<object> Diagnostics { get; private set; }
}
