// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Sequences.for_EventSequenceQueryCriteriaFactory;

public class when_creating_with_named_tags : Specification
{
    Storage.EventSequences.EventSequenceQueryCriteria _criteria;

    void Because() => _criteria = EventSequenceQueryCriteriaFactory.CreateWithNamedTags(
        new(EventSourceId: " source ", Tags: "legacy"),
        [new("account", ["one", "two"]), new("region", AnyValue: true)]);

    [Fact] void should_keep_legacy_narrowing() => _criteria.EventSourceId.Value.ShouldEqual("source");
    [Fact] void should_keep_legacy_tags() => _criteria.Tags.Single().Value.ShouldEqual("legacy");
    [Fact] void should_keep_named_tag_names() => _criteria.NamedTags.Select(_ => _.Name.Value).ShouldContain("account");
    [Fact] void should_keep_exact_values() => _criteria.NamedTags.First().Values.ShouldContain("two");
    [Fact] void should_allow_any_value_for_a_name() => _criteria.NamedTags.Last().Values.ShouldBeNull();
}
