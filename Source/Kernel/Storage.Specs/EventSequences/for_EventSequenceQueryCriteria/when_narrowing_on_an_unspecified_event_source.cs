// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.EventSequences.for_EventSequenceQueryCriteria;

/// <summary>
/// An unspecified event source id is the "do not narrow" sentinel, not a value to compare against.
/// Treating it as a value would narrow every event away and make a query silently return nothing.
/// </summary>
public class when_narrowing_on_an_unspecified_event_source : Specification
{
    static readonly EventSequenceQueryCriteria _criteria = new(EventSourceId.Unspecified);
    bool _result;

    void Because() => _result = _criteria.Matches("some-source", "SomeEventType", [], DateTimeOffset.UtcNow);

    [Fact] void should_match_events_from_any_event_source() => _result.ShouldBeTrue();
    [Fact] void should_not_consider_itself_to_narrow_on_event_source() => _criteria.HasEventSourceId.ShouldBeFalse();
}
