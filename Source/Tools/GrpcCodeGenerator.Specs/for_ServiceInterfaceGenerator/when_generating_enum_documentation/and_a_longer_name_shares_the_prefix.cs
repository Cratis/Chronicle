// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.SharedTypeCatalog;
using Cratis.Chronicle.Tools.GrpcCodeGenerator.for_SharedTypeRegistry;

namespace Cratis.Chronicle.Tools.GrpcCodeGenerator.for_ServiceInterfaceGenerator.when_generating_enum_documentation;

[Collection(SharedTypeRegistryCollection.Name)]
public class and_a_longer_name_shares_the_prefix : given.a_documented_enum_generator
{
    void Because() => _code = _generator.GenerateSharedType(typeof(DocumentedStatus), _outputDir);

    [Fact] void should_leave_literal_text_alone() => _code.ShouldContain("<c language=\"csharp\">Cratis.Chronicle.SharedTypeCatalog.DocumentedStatusExtended</c>");
    [Fact] void should_not_rewrite_the_longer_type_as_a_contract() => _code.ShouldNotContain("Cratis.Chronicle.Contracts.SharedTypeCatalog.DocumentedStatusExtended");
    [Fact] void should_render_the_ungenerated_longer_reference_as_text() => _code.ShouldContain("<c>DocumentedStatusExtended</c>");
    [Fact] void should_not_discover_a_type_just_from_documentation() => SharedTypeRegistry.Discovered.ContainsKey(typeof(DocumentedStatusExtended)).ShouldBeFalse();
}
