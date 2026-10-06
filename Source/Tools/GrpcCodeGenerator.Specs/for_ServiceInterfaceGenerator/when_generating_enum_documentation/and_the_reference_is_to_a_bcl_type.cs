// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Tools.GrpcCodeGenerator.for_SharedTypeRegistry;

namespace Cratis.Chronicle.Tools.GrpcCodeGenerator.for_ServiceInterfaceGenerator.when_generating_enum_documentation;

[Collection(SharedTypeRegistryCollection.Name)]
public class and_the_reference_is_to_a_bcl_type : given.a_fallback_enum_generator
{
    void Because() => Generate();

    [Fact] void should_keep_system_links() => _documentation.Element("summary")!.Elements("see").First().Attribute("cref")!.Value.ShouldEqual("T:System.Xml.Linq.XDocument");
    [Fact] void should_keep_microsoft_links() => _documentation.Element("summary")!.Elements("see").Last().Attribute("cref")!.Value.ShouldEqual("T:Microsoft.CSharp.RuntimeBinder.Binder");
}
