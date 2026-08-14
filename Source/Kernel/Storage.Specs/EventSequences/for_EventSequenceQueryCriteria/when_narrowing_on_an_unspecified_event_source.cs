// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventSequences.for_EventSequenceQueryCriteria.given;

namespace Cratis.Chronicle.Storage.EventSequences.for_EventSequenceQueryCriteria;

/// <summary>
/// Each dimension carries a sentinel that means "do not narrow", not a value to compare against.
/// Treating one as a value would narrow every event away and make a query silently return nothing.
/// </summary>
public class when_narrowing_on_an_unspecified_event_source : Specification
{
    static readonly EventSequenceQueryCriteria _criteria = new(
        EventSourceId.Unspecified,
        EventSourceType.Unspecified,
        EventStreamType.All,
        CorrelationId.NotSet);

    bool _result;

    void Because() => _result = _criteria.Matches(an_event.With("some-source", "SomeEventType"));

    [Fact] void should_match_events_from_any_event_source() => _result.ShouldBeTrue();
    [Fact] void should_not_consider_itself_to_narrow_on_event_source() => _criteria.HasEventSourceId.ShouldBeFalse();
    [Fact] void should_not_consider_itself_to_narrow_on_event_source_type() => _criteria.HasEventSourceType.ShouldBeFalse();
    [Fact] void should_not_consider_itself_to_narrow_on_event_stream_type() => _criteria.HasEventStreamType.ShouldBeFalse();
    [Fact] void should_not_consider_itself_to_narrow_on_correlation() => _criteria.HasCorrelationId.ShouldBeFalse();
}
