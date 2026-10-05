// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Tools.GrpcCodeGenerator.for_SharedTypeRegistry;

namespace Cratis.Chronicle.Tools.GrpcCodeGenerator.for_ServiceInterfaceGenerator.when_generating_enum_documentation;

[Collection(SharedTypeRegistryCollection.Name)]
public class and_an_unresolved_see_is_empty : given.a_fallback_enum_generator
{
    void Because() => Generate();

    [Fact] void should_render_the_simple_name_as_text() => _documentation.Element("summary")!.Elements("c").First().ToString().ShouldEqual("<c>CoreOwnedReadModel</c>");
}
