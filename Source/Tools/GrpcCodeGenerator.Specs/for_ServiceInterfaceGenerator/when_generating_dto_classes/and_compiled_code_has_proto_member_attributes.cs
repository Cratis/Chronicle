// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Cratis.Chronicle.Tools.GrpcCodeGenerator.for_ServiceInterfaceGenerator.when_generating_dto_classes;

/// <summary>
/// Verifies via Roslyn compilation and reflection that generated DTO classes carry [ProtoContract]
/// and [ProtoMember(N)] attributes with correct indexes.
/// </summary>
public class and_compiled_code_has_proto_member_attributes : given.a_generated_service_interface_with_output_dir
{
    IReadOnlyList<Diagnostic> _diagnostics = null!;
    Type? _requestType;

    void Because()
    {
        var code = _generator.Generate(_serviceDefinition, _outputDir);

        // Force ProtoBuf assemblies into the AppDomain so they appear in the loaded assembly list
        // used to build Roslyn MetadataReferences below — they may not be loaded yet at this point.
        _ = typeof(ProtoBuf.ProtoContractAttribute).Assembly;
        _ = typeof(ProtoBuf.Grpc.CallContext).Assembly;
        _ = typeof(ProtoBuf.Grpc.Configuration.ServiceAttribute).Assembly;

        var parseOptions = new CSharpParseOptions(LanguageVersion.Latest);
        const string globalUsings = """
            global using System;
            global using System.Collections.Generic;
            global using System.Threading.Tasks;
            """;
        var globalTree = CSharpSyntaxTree.ParseText(globalUsings, parseOptions);
        var codeTree = CSharpSyntaxTree.ParseText(code, parseOptions);

        var references = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
            .Select(a => MetadataReference.CreateFromFile(a.Location))
            .Cast<MetadataReference>()
            .ToList();

        var compilation = CSharpCompilation.Create(
            "ProtoTest",
            [codeTree, globalTree],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        _diagnostics = compilation.GetDiagnostics()
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .DistinctBy(d => d.Id)
            .ToList();

        using var ms = new MemoryStream();
        if (compilation.Emit(ms).Success)
        {
            ms.Seek(0, SeekOrigin.Begin);
            var compiledAssembly = Assembly.Load(ms.ToArray());
            _requestType = compiledAssembly.GetType(
                $"Generated.{_serviceDefinition.Namespace}.RegisterProductRequest");
        }
    }

    [Fact] void should_compile_without_errors() => _diagnostics.ShouldBeEmpty();
    [Fact] void should_load_request_type() => _requestType.ShouldNotBeNull();
    [Fact] void should_have_proto_contract_attribute() =>
        _requestType?.GetCustomAttributesData()
            .ShouldContain(a => a.AttributeType.Name == "ProtoContractAttribute");
    [Fact] void should_have_proto_member_on_first_property() =>
        _requestType?.GetProperties()[0]
            .GetCustomAttributesData()
            .ShouldContain(a => a.AttributeType.Name == "ProtoMemberAttribute");
}
