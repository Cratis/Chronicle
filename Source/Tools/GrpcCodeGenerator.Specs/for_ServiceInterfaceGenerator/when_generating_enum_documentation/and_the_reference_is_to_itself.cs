// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.SharedTypeCatalog;
using Cratis.Chronicle.Tools.GrpcCodeGenerator.for_SharedTypeRegistry;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Cratis.Chronicle.Tools.GrpcCodeGenerator.for_ServiceInterfaceGenerator.when_generating_enum_documentation;

[Collection(SharedTypeRegistryCollection.Name)]
public class and_the_reference_is_to_itself : given.a_documented_enum_generator
{
    IReadOnlyList<Diagnostic> _diagnostics = null!;

    void Because()
    {
        _code = _generator.GenerateSharedType(typeof(DocumentedStatus), _outputDir);
        var sharedCode = _generator.GenerateSharedType(typeof(CoreOwnedStatus), _outputDir);
        var parseOptions = new CSharpParseOptions(documentationMode: DocumentationMode.Diagnose);
        var compilation = CSharpCompilation.Create(
            "DocumentedContracts",
            new[] { _code, sharedCode }.Select(code => CSharpSyntaxTree.ParseText(code, parseOptions)),
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        _diagnostics = compilation.GetDiagnostics();
    }

    [Fact] void should_rewrite_the_type_reference() => _code.ShouldContain("cref=\"T:Cratis.Chronicle.Contracts.SharedTypeCatalog.DocumentedStatus\"");
    [Fact] void should_rewrite_the_member_reference() => _code.ShouldContain("cref=\"F:Cratis.Chronicle.Contracts.SharedTypeCatalog.DocumentedStatus.First\"");
    [Fact] void should_not_put_global_aliases_in_documentation_ids() => _code.ShouldNotContain("cref=\"T:global::");
    [Fact] void should_rewrite_another_shared_type_reference() => _code.ShouldContain("cref=\"T:Cratis.Chronicle.Contracts.SharedTypeCatalog.CoreOwnedStatus\"");
    [Fact] void should_compile_with_valid_documentation() => _diagnostics.ShouldBeEmpty();
}
