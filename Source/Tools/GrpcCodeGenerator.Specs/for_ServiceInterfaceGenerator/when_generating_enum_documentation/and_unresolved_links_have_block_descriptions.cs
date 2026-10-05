// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.SharedTypeCatalog;
using Cratis.Chronicle.Tools.GrpcCodeGenerator.for_SharedTypeRegistry;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Cratis.Chronicle.Tools.GrpcCodeGenerator.for_ServiceInterfaceGenerator.when_generating_enum_documentation;

[Collection(SharedTypeRegistryCollection.Name)]
public class and_unresolved_links_have_block_descriptions : when_generating_a_shared_type.given.a_shared_type_generator
{
    IReadOnlyList<Diagnostic> _diagnostics = null!;

    void Because()
    {
        var code = _generator.GenerateSharedType(typeof(DocumentedFallbackStatus), _outputDir);
        var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator)
            .Select(path => MetadataReference.CreateFromFile(path));
        var compilation = CSharpCompilation.Create(
            "FallbackContracts",
            [CSharpSyntaxTree.ParseText(code, new CSharpParseOptions(documentationMode: DocumentationMode.Diagnose))],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        _diagnostics = compilation.GetDiagnostics();
    }

    [Fact] void should_compile_with_valid_documentation() => _diagnostics.ShouldBeEmpty();
}
