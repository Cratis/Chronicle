// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Events.for_EventContext;

public class when_using_legacy_context
{
    [Fact] void should_default_to_no_named_tags() => EventContext.Empty.NamedTags.ShouldBeEmpty();
    [Fact] void should_treat_explicit_null_as_empty() => (EventContext.Empty with { NamedTags = null! }).NamedTags.ShouldBeEmpty();
    [Fact] void should_reject_null_named_tag() => Catch.Exception(() => _ = EventContext.Empty with { NamedTags = [null!] }).ShouldBeOfExactType<InvalidNamedTag>();
    [Fact] void should_snapshot_and_expose_read_only_named_tags()
    {
        var tags = new List<NamedTag> { new("name", "v") };
        var context = EventContext.Empty with { NamedTags = tags };
        tags.Clear();
        context.NamedTags.Single().Value.ShouldEqual("v");
        ((IList<NamedTag>)context.NamedTags).IsReadOnly.ShouldBeTrue();
    }
    [Fact] void should_keep_sixteen_positional_constructor_parameters() => typeof(EventContext).GetConstructors().Single().GetParameters().Length.ShouldEqual(16);
    [Fact] void should_keep_sixteen_deconstructed_values() => typeof(EventContext).GetMethod("Deconstruct")!.GetParameters().Length.ShouldEqual(16);
    [Fact] void should_preserve_named_tags_on_record_copy() =>
        (EventContext.Empty with { NamedTags = [new NamedTag("n", "v")] } with { Tags = [] }).NamedTags.Single().Value.ShouldEqual("v");
}
