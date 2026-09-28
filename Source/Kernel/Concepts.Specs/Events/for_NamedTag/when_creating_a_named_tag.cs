// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Events.for_NamedTag;

public class when_creating_a_named_tag
{
    [Fact] void should_convert_name_both_ways() => ((string)(TagName)"some:name").ShouldEqual("some:name");

    [Fact] void should_accept_an_empty_value() => new NamedTag("a", string.Empty).Value.ShouldBeEmpty();
    [Fact] void should_preserve_an_opaque_value() => new NamedTag("a:b", "one:two/three").Value.ShouldEqual("one:two/three");
    [Fact] void should_reject_an_unset_name() => Catch.Exception(() => _ = new NamedTag(TagName.NotSet, "v")).ShouldBeOfExactType<InvalidNamedTag>();
    [Fact] void should_reject_a_whitespace_name() => Catch.Exception(() => _ = new NamedTag("  ", "v")).ShouldBeOfExactType<InvalidNamedTag>();
    [Fact] void should_reject_a_null_name() => Catch.Exception(() => _ = new NamedTag(null!, "v")).ShouldBeOfExactType<InvalidNamedTag>();
    [Fact] void should_reject_a_null_value() => Catch.Exception(() => _ = new NamedTag("n", null!)).ShouldBeOfExactType<InvalidNamedTag>();
    [Fact] void should_reject_an_invalid_name_on_copy() => Catch.Exception(() => _ = new NamedTag("n", "v") with { Name = TagName.NotSet }).ShouldBeOfExactType<InvalidNamedTag>();
    [Fact] void should_reject_a_null_value_on_copy() => Catch.Exception(() => _ = new NamedTag("n", "v") with { Value = null! }).ShouldBeOfExactType<InvalidNamedTag>();
}
