// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.SharedTypeCatalog;
using Cratis.Chronicle.Tools.GrpcCodeGenerator.for_SharedTypeRegistry;

namespace Cratis.Chronicle.Tools.GrpcCodeGenerator.for_ServiceInterfaceGenerator.when_generating_enum_documentation;

[Collection(SharedTypeRegistryCollection.Name)]
public class and_a_shared_type_is_discovered_through_a_later_type : when_generating_a_shared_type.given.a_shared_type_generator
{
    string _code = null!;

    void Establish()
    {
        SharedTypeRegistry.QualifiedNameFor(typeof(DocumentedStatus));
        SharedTypeRegistry.QualifiedNameFor(typeof(DocumentationStatusHolder));
    }

    void Because() => _code = _generator.GenerateSharedType(typeof(DocumentedStatus), _outputDir);

    [Fact] void should_link_to_the_transitively_discovered_shared_type() => _code.ShouldContain("cref=\"T:Cratis.Chronicle.Contracts.SharedTypeCatalog.CoreOwnedStatus\"");
    [Fact] void should_not_render_the_shared_type_as_text() => _code.ShouldNotContain("<c>CoreOwnedStatus</c>");
}
