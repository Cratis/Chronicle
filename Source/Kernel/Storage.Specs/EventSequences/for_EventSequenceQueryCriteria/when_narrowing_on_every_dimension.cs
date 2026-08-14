// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventSequences.for_EventSequenceQueryCriteria.given;

namespace Cratis.Chronicle.Storage.EventSequences.for_EventSequenceQueryCriteria;

public class when_narrowing_on_every_dimension
{
    static readonly DateTimeOffset _from = new(2026, 8, 1, 0, 0, 0, TimeSpan.Zero);
    static readonly DateTimeOffset _to = new(2026, 8, 2, 0, 0, 0, TimeSpan.Zero);
    static readonly CorrelationId _correlationId = new(Guid.Parse("2f3b3a0e-0000-4000-8000-000000000001"));
    static readonly EventSourceType _eventSourceType = new("TheSourceType");
    static readonly EventStreamType _eventStreamType = new("TheStreamType");

    static readonly EventSequenceQueryCriteria _criteria = new(
        "the-source",
        _eventSourceType,
        _eventStreamType,
        _correlationId,
        [new EventType("TheEventType", EventTypeGeneration.First)],
        [new Tag("the-tag")],
        _from,
        _to);

    static EventContext Matching(
        string eventSourceId = "the-source",
        string eventType = "TheEventType",
        string tag = "the-tag",
        DateTimeOffset? occurred = null,
        EventSourceType? eventSourceType = null,
        EventStreamType? eventStreamType = null,
        CorrelationId? correlationId = null) =>
        an_event.With(
            eventSourceId,
            eventType,
            [tag],
            occurred ?? _from,
            eventSourceType ?? _eventSourceType,
            eventStreamType ?? _eventStreamType,
            correlationId ?? _correlationId);

    [Fact] void should_match_an_event_meeting_every_criterion() =>
        _criteria.Matches(Matching()).ShouldBeTrue();

    [Fact] void should_not_match_an_event_from_another_event_source() =>
        _criteria.Matches(Matching(eventSourceId: "another-source")).ShouldBeFalse();

    [Fact] void should_not_match_an_event_of_another_type() =>
        _criteria.Matches(Matching(eventType: "AnotherEventType")).ShouldBeFalse();

    [Fact] void should_not_match_an_event_of_another_event_source_type() =>
        _criteria.Matches(Matching(eventSourceType: new EventSourceType("AnotherSourceType"))).ShouldBeFalse();

    [Fact] void should_not_match_an_event_on_another_event_stream_type() =>
        _criteria.Matches(Matching(eventStreamType: new EventStreamType("AnotherStreamType"))).ShouldBeFalse();

    [Fact] void should_not_match_an_event_from_another_correlation() =>
        _criteria.Matches(Matching(correlationId: CorrelationId.New())).ShouldBeFalse();

    [Fact] void should_not_match_an_event_without_any_of_the_tags() =>
        _criteria.Matches(Matching(tag: "another-tag")).ShouldBeFalse();

    [Fact] void should_not_match_an_event_occurring_before_the_lower_bound() =>
        _criteria.Matches(Matching(occurred: _from.AddTicks(-1))).ShouldBeFalse();

    [Fact] void should_match_an_event_occurring_exactly_on_the_lower_bound() =>
        _criteria.Matches(Matching(occurred: _from)).ShouldBeTrue();

    [Fact] void should_not_match_an_event_occurring_exactly_on_the_upper_bound() =>
        _criteria.Matches(Matching(occurred: _to)).ShouldBeFalse();

    [Fact] void should_match_an_event_occurring_just_before_the_upper_bound() =>
        _criteria.Matches(Matching(occurred: _to.AddTicks(-1))).ShouldBeTrue();

    [Fact] void should_match_an_event_carrying_the_tag_among_others() =>
        _criteria.Matches(an_event.With(
            "the-source",
            "TheEventType",
            ["another-tag", "the-tag"],
            _from,
            _eventSourceType,
            _eventStreamType,
            _correlationId)).ShouldBeTrue();

    [Fact] void should_ignore_the_generation_when_matching_event_types() =>
        new EventSequenceQueryCriteria(EventTypes: [new EventType("TheEventType", new EventTypeGeneration(7))])
            .Matches(an_event.With("the-source", "TheEventType")).ShouldBeTrue();
}
