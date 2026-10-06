// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Tools.GrpcCodeGenerator.for_SharedTypeRegistry;

namespace Cratis.Chronicle.Tools.GrpcCodeGenerator.for_ServiceInterfaceGenerator.when_generating_enum_documentation;

[Collection(SharedTypeRegistryCollection.Name)]
public class and_an_unresolved_exception_has_a_description : given.a_fallback_enum_generator
{
    void Because() => Generate();

    [Fact] void should_keep_the_description_in_remarks() => _documentation.Elements("remarks").Last().Element("para")!.ToString().ShouldEqual("<para>An exception <b>description</b> with <c>nested text</c>.</para>");
    [Fact] void should_remove_the_invalid_exception_element() => _documentation.Elements("exception").ShouldBeEmpty();
    [Fact] void should_not_emit_top_level_inline_code() => _documentation.Elements("c").ShouldBeEmpty();
}
