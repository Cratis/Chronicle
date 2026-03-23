// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Cratis.Chronicle.Tools.GrpcCodeGenerator.for_ServiceInterfaceGenerator.when_generating_for_a_service_with_commands_and_queries;

/// <summary>
/// Verifies that the generator produces code that compiles cleanly and exposes the expected interface shape.
/// </summary>
public class and_compiling_the_generated_code : given.a_generated_service_interface
{
    IReadOnlyList<Diagnostic> _diagnostics = null!;
    Assembly _compiledAssembly = null!;
    Type _serviceInterface = null!;
    string _interfaceTypeName = null!;

    void Because()
    {
        var outputDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(outputDir);
        _generatedCode = _generator.Generate(_serviceDefinition, outputDir);

        // Force-load assemblies that the generated code depends on so they appear in AppDomain
        _ = typeof(ProtoBuf.Grpc.CallContext).Assembly;
        _ = typeof(ProtoBuf.Grpc.Configuration.ServiceAttribute).Assembly;

        var parseOptions = new CSharpParseOptions(LanguageVersion.Latest);

        const string globalUsingsSource = """
            global using System;
            global using System.Collections.Generic;
            global using System.Threading.Tasks;
            """;
        var globalUsingsTree = CSharpSyntaxTree.ParseText(globalUsingsSource, parseOptions);
        var codeTree = CSharpSyntaxTree.ParseText(_generatedCode, parseOptions);

        var references = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
            .Select(a => MetadataReference.CreateFromFile(a.Location))
            .Cast<MetadataReference>()
            .ToList();

        var compilation = CSharpCompilation.Create(
            "GeneratedTest",
            [codeTree, globalUsingsTree],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        using var ms = new MemoryStream();
        var emitResult = compilation.Emit(ms);

        // Collect all errors from both semantic analysis and emit
        _diagnostics = compilation.GetDiagnostics()
            .Concat(emitResult.Diagnostics)
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .Distinct()
            .ToList();

        if (emitResult.Success)
        {
            ms.Seek(0, SeekOrigin.Begin);
            _compiledAssembly = Assembly.Load(ms.ToArray());

            // Construct the type name from the generator's baseNamespace + service namespace + service name
            _interfaceTypeName = $"Generated.{_serviceDefinition.Namespace}.I{_serviceDefinition.ServiceName}";
            _serviceInterface = _compiledAssembly.GetType(_interfaceTypeName)!;
        }
    }

    [Fact] void should_compile_without_errors() => _diagnostics.ShouldBeEmpty();
    [Fact] void should_have_service_interface() => _serviceInterface.ShouldNotBeNull();
    [Fact] void should_have_register_product_method() => _serviceInterface.GetMethod("RegisterProduct").ShouldNotBeNull();
    [Fact] void should_have_get_all_method() => _serviceInterface.GetMethod("GetAll").ShouldNotBeNull();
}
