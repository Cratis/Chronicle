// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueue.when_enqueuing;

public class multiple_events_with_different_partitions_with_single_subscriber : given.a_single_subscriber_and_two_event_types
{
    async Task Because()
    {
        await _queue.Enqueue([_firstAppendedEvent, _secondAppendedEvent]);
        await _queue.AwaitQueueDepletion();
    }

    [Fact] void should_call_handle_on_observer_for_two_different_partitions() => _handledEventsPerPartition.Count.ShouldEqual(2);
    [Fact] void should_call_handle_on_observer_for_two_events() => _handledEventsPerPartition.Sum(_ => _.Value.Count).ShouldEqual(2);
    [Fact] void should_call_handle_on_observer_with_correct_event_source_id_for_first_event() => _handledEventsPerPartition[_firstEventSourceId].SingleOrDefault(e => e.Partition == _firstEventSourceId.Value).ShouldNotBeNull();
    [Fact] void should_call_handle_on_observer_with_correct_event_source_id_for_second_event() => _handledEventsPerPartition[_secondEventSourceId].SingleOrDefault(e => e.Partition == _secondEventSourceId.Value).ShouldNotBeNull();
    [Fact] void should_call_handle_on_observer_with_correct_event_for_first_event() => _handledEventsPerPartition[_firstEventSourceId].SingleOrDefault(e => e.Events.SingleOrDefault(e => e == _firstAppendedEvent) is not null).ShouldNotBeNull();
    [Fact] void should_call_handle_on_observer_with_correct_event_for_second_event() => _handledEventsPerPartition[_secondEventSourceId].SingleOrDefault(e => e.Events.SingleOrDefault(e => e == _secondAppendedEvent) is not null).ShouldNotBeNull();
}
