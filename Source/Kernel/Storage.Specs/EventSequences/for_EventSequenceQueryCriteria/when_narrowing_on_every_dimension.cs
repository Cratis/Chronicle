// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.EventSequences.for_EventSequenceQueryCriteria;

public class when_narrowing_on_every_dimension
{
    static readonly DateTimeOffset _from = new(2026, 8, 1, 0, 0, 0, TimeSpan.Zero);
    static readonly DateTimeOffset _to = new(2026, 8, 2, 0, 0, 0, TimeSpan.Zero);
    static readonly EventSequenceQueryCriteria _criteria = new(
        "the-source",
        [new EventType("TheEventType", EventTypeGeneration.First)],
        [new Tag("the-tag")],
        _from,
        _to);

    [Fact] void should_match_an_event_meeting_every_criterion() =>
        _criteria.Matches("the-source", "TheEventType", ["the-tag"], _from).ShouldBeTrue();

    [Fact] void should_not_match_an_event_from_another_event_source() =>
        _criteria.Matches("another-source", "TheEventType", ["the-tag"], _from).ShouldBeFalse();

    [Fact] void should_not_match_an_event_of_another_type() =>
        _criteria.Matches("the-source", "AnotherEventType", ["the-tag"], _from).ShouldBeFalse();

    [Fact] void should_not_match_an_event_without_any_of_the_tags() =>
        _criteria.Matches("the-source", "TheEventType", ["another-tag"], _from).ShouldBeFalse();

    [Fact] void should_not_match_an_event_occurring_before_the_lower_bound() =>
        _criteria.Matches("the-source", "TheEventType", ["the-tag"], _from.AddTicks(-1)).ShouldBeFalse();

    [Fact] void should_match_an_event_occurring_exactly_on_the_lower_bound() =>
        _criteria.Matches("the-source", "TheEventType", ["the-tag"], _from).ShouldBeTrue();

    [Fact] void should_not_match_an_event_occurring_exactly_on_the_upper_bound() =>
        _criteria.Matches("the-source", "TheEventType", ["the-tag"], _to).ShouldBeFalse();

    [Fact] void should_match_an_event_occurring_just_before_the_upper_bound() =>
        _criteria.Matches("the-source", "TheEventType", ["the-tag"], _to.AddTicks(-1)).ShouldBeTrue();

    [Fact] void should_match_an_event_carrying_the_tag_among_others() =>
        _criteria.Matches("the-source", "TheEventType", ["another-tag", "the-tag"], _from).ShouldBeTrue();

    [Fact] void should_ignore_the_generation_when_matching_event_types() =>
        new EventSequenceQueryCriteria(EventTypes: [new EventType("TheEventType", new EventTypeGeneration(7))])
            .Matches("the-source", "TheEventType", [], _from).ShouldBeTrue();
}
