// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Tools.GrpcCodeGenerator.for_SharedTypeRegistry;

namespace Cratis.Chronicle.Tools.GrpcCodeGenerator.for_ServiceInterfaceGenerator.when_generating_enum_documentation;

[Collection(SharedTypeRegistryCollection.Name)]
public class and_an_unresolved_inheritdoc_has_other_attributes : given.a_fallback_enum_generator
{
    void Because() => Generate();

    [Fact] void should_remove_only_the_cref_attribute() => _documentation.Element("inheritdoc")!.ToString().ShouldEqual("<inheritdoc path=\"/summary\" />");
}
