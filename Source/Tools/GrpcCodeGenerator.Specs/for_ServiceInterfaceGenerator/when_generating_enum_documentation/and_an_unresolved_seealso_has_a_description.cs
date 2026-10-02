// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Tools.GrpcCodeGenerator.for_SharedTypeRegistry;

namespace Cratis.Chronicle.Tools.GrpcCodeGenerator.for_ServiceInterfaceGenerator.when_generating_enum_documentation;

[Collection(SharedTypeRegistryCollection.Name)]
public class and_an_unresolved_seealso_has_a_description : given.a_fallback_enum_generator
{
    void Because() => Generate();

    [Fact] void should_keep_the_description_in_remarks() => _documentation.Elements("remarks").First().Element("para")!.ToString().ShouldEqual("<para>related <b>description</b></para>");
    [Fact] void should_remove_unresolved_seealso_elements() => _documentation.Elements("seealso").ShouldBeEmpty();
    [Fact] void should_not_add_a_description_for_an_empty_link() => _documentation.Elements("remarks").Count().ShouldEqual(2);
}
