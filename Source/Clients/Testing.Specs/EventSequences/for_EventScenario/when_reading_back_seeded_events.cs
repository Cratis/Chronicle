// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.EventSequences.for_EventScenario;

public class when_reading_back_seeded_events : Specification, IDisposable
{
    EventScenario _scenario;
    EventSourceId _eventSourceId;
    EventSourceId _otherEventSourceId;
    IImmutableList<AppendedEvent> _all;
    IImmutableList<AppendedEvent> _forEventSource;

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

        _all = await _scenario.EventSequence.GetFromSequenceNumber(EventSequenceNumber.First);
        _forEventSource = await _scenario.EventSequence.GetFromSequenceNumber(EventSequenceNumber.First, _eventSourceId);
    }

    [Fact] void should_read_back_every_seeded_event() => _all.Count.ShouldEqual(3);
    [Fact] void should_read_back_only_the_events_for_the_requested_event_source() => _forEventSource.Count.ShouldEqual(2);
    [Fact] async Task should_report_the_tail_sequence_number() => await _scenario.EventSequence.ShouldHaveTailSequenceNumber(2);
    [Fact] async Task should_find_the_seeded_event_by_type() => await _scenario.EventSequence.ShouldHaveAppendedEvent<TestEvent>(_eventSourceId, _ => _.Value == "second");

    public void Dispose() => _scenario.Dispose();
}
