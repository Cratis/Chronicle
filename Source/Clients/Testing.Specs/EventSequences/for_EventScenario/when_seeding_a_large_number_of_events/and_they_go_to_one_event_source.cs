// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.EventSequences.for_EventScenario.when_seeding_a_large_number_of_events;

public class and_they_go_to_one_event_source : Specification, IDisposable
{
    const int NumberOfEvents = 1000;

    EventScenario _scenario;
    EventSourceId _eventSourceId;
    IImmutableList<AppendedEvent> _appended;

    void Establish()
    {
        _scenario = new EventScenario();
        _eventSourceId = EventSourceId.New();
    }

    async Task Because()
    {
        await _scenario.Given
            .ForEventSource(_eventSourceId)
            .Events(Enumerable.Range(0, NumberOfEvents).Select(_ => new TestEvent($"event {_}")).ToArray<object>());

        _appended = await _scenario.EventSequence.GetFromSequenceNumber(EventSequenceNumber.First, _eventSourceId);
    }

    [Fact] void should_have_appended_every_event() => _appended.Count.ShouldEqual(NumberOfEvents);
    [Fact] async Task should_have_the_last_event_at_the_tail() => await _scenario.EventSequence.ShouldHaveTailSequenceNumber(NumberOfEvents - 1);
    [Fact] void should_keep_the_events_in_seed_order() => _appended.Select(_ => ((TestEvent)_.Content).Value).ShouldEqual(Enumerable.Range(0, NumberOfEvents).Select(_ => $"event {_}"));
    [Fact] void should_number_the_events_contiguously() => _appended.Select(_ => (ulong)_.Context.SequenceNumber).ShouldEqual(Enumerable.Range(0, NumberOfEvents).Select(_ => (ulong)_));

    public void Dispose() => _scenario.Dispose();
}
