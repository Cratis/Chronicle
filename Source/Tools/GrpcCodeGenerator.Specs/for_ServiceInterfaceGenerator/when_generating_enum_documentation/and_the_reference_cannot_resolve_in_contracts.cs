// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.SharedTypeCatalog;
using Cratis.Chronicle.Tools.GrpcCodeGenerator.for_SharedTypeRegistry;

namespace Cratis.Chronicle.Tools.GrpcCodeGenerator.for_ServiceInterfaceGenerator.when_generating_enum_documentation;

[Collection(SharedTypeRegistryCollection.Name)]
public class and_the_reference_cannot_resolve_in_contracts : given.a_documented_enum_generator
{
    void Because() => _code = _generator.GenerateSharedType(typeof(DocumentedStatus), _outputDir);

    [Fact] void should_render_the_core_only_reference_as_text() => _code.ShouldContain("<c>CoreOwnedReadModel</c>");
    [Fact] void should_remove_the_core_only_cref() => _code.ShouldNotContain("cref=\"T:Cratis.Chronicle.SharedTypeCatalog.CoreOwnedReadModel\"");
    [Fact] void should_preserve_bcl_type_references() => _code.ShouldContain("cref=\"T:System.DateTime\"");
    [Fact] void should_preserve_bcl_member_references() => _code.ShouldContain("cref=\"P:System.DateTime.UtcNow\"");
}
