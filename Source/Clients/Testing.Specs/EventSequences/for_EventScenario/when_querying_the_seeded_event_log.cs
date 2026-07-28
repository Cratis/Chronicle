// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.EventSequences.for_EventScenario;

/// <summary>
/// Covers the read surface reachable from <see cref="EventScenario"/> beyond
/// <c>GetFromSequenceNumber</c>. Every one of these narrows through the same storage filter, so a
/// regression in how "no filter" sentinels are treated silently empties them all.
/// </summary>
public class when_querying_the_seeded_event_log : Specification, IDisposable
{
    EventScenario _scenario;
    EventSourceId _eventSourceId;
    EventSourceId _otherEventSourceId;
    bool _hasEventsForSeededSource;
    bool _hasEventsForUnknownSource;
    EventSequenceNumber _nextSequenceNumber;
    IImmutableList<AppendedEvent> _byEventType;

    void Establish()
    {
        _scenario = new EventScenario();
        _eventSourceId = EventSourceId.New();
        _otherEventSourceId = EventSourceId.New();
    }

    async Task Because()
    {
        await _scenario.Given.ForEventSource(_eventSourceId).Events(new TestEvent("first"), new TestEvent("second"));
        await _scenario.Given.ForEventSource(_otherEventSourceId).Events(new TestEvent("other"));

        _hasEventsForSeededSource = await _scenario.EventSequence.HasEventsFor(_eventSourceId);
        _hasEventsForUnknownSource = await _scenario.EventSequence.HasEventsFor(EventSourceId.New());
        _nextSequenceNumber = await _scenario.EventSequence.GetNextSequenceNumber();
        _byEventType = await _scenario.EventSequence.GetForEventSourceIdAndEventTypes(
            _eventSourceId,
            [Defaults.Instance.EventTypes.GetEventTypeFor(typeof(TestEvent))]);
    }

    [Fact] void should_report_events_for_the_seeded_event_source() => _hasEventsForSeededSource.ShouldBeTrue();
    [Fact] void should_not_report_events_for_an_unknown_event_source() => _hasEventsForUnknownSource.ShouldBeFalse();
    [Fact] void should_report_the_next_sequence_number_after_the_tail() => _nextSequenceNumber.ShouldEqual((EventSequenceNumber)3UL);
    [Fact] void should_return_the_events_of_the_requested_type_for_that_event_source() => _byEventType.Count.ShouldEqual(2);

    public void Dispose() => _scenario.Dispose();
}
