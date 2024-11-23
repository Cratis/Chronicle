// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Grains.EventSequences.for_AppendedEventsQueue.when_enqueuing;

public class multiple_events_with_different_partitions_with_single_subscriber : given.a_single_subscriber
{
    EventType _firstEventType = new("Some event", 1);
    EventType _secondEventType = new("Some other event", 2);
    AppendedEvent _firstAppendedEvent;
    AppendedEvent _secondAppendedEvent;

    EventSourceId _firstEventSourceId;
    EventSourceId _secondEventSourceId;

    void Establish()
    {
        _firstAppendedEvent = AppendedEvent.EmptyWithEventType(_firstEventType);
        _firstEventSourceId = Guid.NewGuid();
        _firstAppendedEvent = _firstAppendedEvent with { Context = EventContext.Empty with { EventSourceId = _firstEventSourceId } };

        _secondAppendedEvent = AppendedEvent.EmptyWithEventType(_secondEventType);
        _secondEventSourceId = Guid.NewGuid();
        _secondAppendedEvent = _secondAppendedEvent with { Context = EventContext.Empty with { EventSourceId = _secondEventSourceId } };
    }

    protected override IEnumerable<EventType> EventTypes => [_firstEventType, _secondEventType];

    async Task Because()
    {
        await _queue.Enqueue([_firstAppendedEvent, _secondAppendedEvent]);
        await _queue.AwaitQueueDepletion();
    }

    [Fact] void should_call_handle_on_observer_twice() => _handledEvents.Count.ShouldEqual(2);
    [Fact] void should_call_handle_on_observer_with_correct_event_source_id_for_first_event() => _handledEvents[0].Partition.Value.ShouldEqual(_firstEventSourceId.Value);
    [Fact] void should_call_handle_on_observer_with_correct_event_source_id_for_second_event() => _handledEvents[1].Partition.Value.ShouldEqual(_secondEventSourceId.Value);
    [Fact] void should_call_handle_on_observer_with_correct_event_for_first_event() => _handledEvents[0].Events.ShouldContainOnly(_firstAppendedEvent);
    [Fact] void should_call_handle_on_observer_with_correct_event_for_second_event() => _handledEvents[1].Events.ShouldContainOnly(_secondAppendedEvent);
}
