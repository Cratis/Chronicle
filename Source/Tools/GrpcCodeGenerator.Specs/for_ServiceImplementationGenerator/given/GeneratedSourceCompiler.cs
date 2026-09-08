// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Cratis.Chronicle.Tools.GrpcCodeGenerator.for_ServiceImplementationGenerator.given;

/// <summary>
/// Compiles generated sources together so the specs can assert on the result rather than on the text.
/// </summary>
public static class GeneratedSourceCompiler
{
    const string GlobalUsings = """
        global using System;
        global using System.Collections.Generic;
        global using System.Linq;
        global using System.Threading.Tasks;
        """;

    /// <summary>
    /// Compiles the given sources alongside the executor stubs.
    /// </summary>
    /// <param name="sources">The generated sources to compile.</param>
    /// <returns>The errors, and the assembly when there were none.</returns>
    public static (IReadOnlyList<Diagnostic> Errors, Assembly? Assembly) Compile(params string[] sources)
    {
        var parseOptions = new CSharpParseOptions(LanguageVersion.Latest);

        SyntaxTree[] trees =
        [
            CSharpSyntaxTree.ParseText(GlobalUsings, parseOptions),
            CSharpSyntaxTree.ParseText(ExecutorStubs.Source, parseOptions),
            .. sources.Select(source => CSharpSyntaxTree.ParseText(source, parseOptions))
        ];

        var compilation = CSharpCompilation.Create(
            "GeneratedImplementationTest",
            trees,
            References(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        using var stream = new MemoryStream();
        var result = compilation.Emit(stream);

        var errors = compilation.GetDiagnostics()
            .Concat(result.Diagnostics)
            .Where(_ => _.Severity == DiagnosticSeverity.Error)
            .DistinctBy(_ => $"{_.Id}:{_.GetMessage()}")
            .ToList();

        return (errors, result.Success ? Assembly.Load(stream.ToArray()) : null);
    }

    static List<MetadataReference> References()
    {
        // These assemblies carry types the generated code names but nothing in this process has touched yet,
        // so they would be missing from the reference set enumerated below.
        _ = typeof(ProtoBuf.ProtoContractAttribute).Assembly;
        _ = typeof(ProtoBuf.Grpc.CallContext).Assembly;
        _ = typeof(ProtoBuf.Grpc.Configuration.ServiceAttribute).Assembly;
        _ = typeof(Contracts.Commands.CommandResult).Assembly;
        _ = typeof(Microsoft.Extensions.Logging.ILogger<>).Assembly;
        _ = typeof(System.Reactive.Linq.Observable).Assembly;
        _ = typeof(System.Linq.Expressions.Expression).Assembly;

        // Cratis.Reactive, which the observable queries complete through, lives in Fundamentals - the same
        // assembly the fixture's concepts derive from.
        _ = typeof(TestAssembly.Catalog.ProductId).BaseType!.Assembly;

        return [.. AppDomain.CurrentDomain.GetAssemblies()
            .Where(_ => !_.IsDynamic && !string.IsNullOrEmpty(_.Location))
            .Select(_ => (MetadataReference)MetadataReference.CreateFromFile(_.Location))];
    }
}
