// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueue.when_enqueuing;

public class multiple_events_with_same_partition_with_single_subscriber : given.a_single_subscriber_and_two_event_types
{
    EventSourceId _eventSourceId;

    void Establish()
    {
        _eventSourceId = Guid.NewGuid();
        _firstAppendedEvent = _firstAppendedEvent with
        {
            Context = _firstAppendedEvent.Context with
            {
                EventSourceId = _eventSourceId,
                SequenceNumber = 0UL
            }
        };

        _secondAppendedEvent = _secondAppendedEvent with
        {
            Context = _secondAppendedEvent.Context with
            {
                EventSourceId = _eventSourceId,
                SequenceNumber = 1UL
            }
        };
    }

    async Task Because()
    {
        await _queue.Enqueue([_firstAppendedEvent, _secondAppendedEvent]);
        await _queue.AwaitQueueDepletion();
    }

    [Fact] void should_call_handle_on_observer_once() => _handledEventsPerPartition[_eventSourceId].Count.ShouldEqual(1);
    [Fact] void should_call_handle_on_observer_with_correct_events() => _handledEventsPerPartition[_eventSourceId][0].Events.ShouldContainOnly(_firstAppendedEvent, _secondAppendedEvent);
    [Fact] void should_not_materialize_filtered_events_as_a_list() => _handledEventsPerPartition[_eventSourceId][0].Events.GetType().ShouldNotEqual(typeof(List<AppendedEvent>));
}
