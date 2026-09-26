// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Sequences.for_EventSequenceQueryCriteriaFactory;

public class when_creating_from_json_named_tags : Specification
{
    Storage.EventSequences.EventSequenceQueryCriteria _criteria;

    void Because() => _criteria = EventSequenceQueryCriteriaFactory.CreateWithNamedTags(
        new(Tags: "legacy"),
        null,
        "[{\"name\":\"account\",\"values\":[\"one\",\"two\"]},{\"name\":\"region\",\"anyValue\":true}]");

    [Fact] void should_keep_the_legacy_dimension() => _criteria.Tags.Single().Value.ShouldEqual("legacy");
    [Fact] void should_keep_named_tag_values() => _criteria.NamedTags.First().Values.ShouldContain("two");
    [Fact] void should_allow_a_name_only_criterion() => _criteria.NamedTags.Last().Values.ShouldBeNull();
}
