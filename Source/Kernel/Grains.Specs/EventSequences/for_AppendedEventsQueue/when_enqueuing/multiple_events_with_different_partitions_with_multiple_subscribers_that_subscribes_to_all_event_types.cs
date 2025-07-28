// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Grains.EventSequences.for_AppendedEventsQueue.when_enqueuing;

public class multiple_events_with_different_partitions_with_multiple_subscribers_that_subscribes_to_all_event_types : given.two_subscribed_subscribers_and_two_event_types
{
    async Task Because()
    {
        await _queue.Enqueue([_firstAppendedEvent, _secondAppendedEvent]);
        await _queue.AwaitQueueDepletion();
    }

    [Fact] void should_call_handle_on_first_observer_twice() => _firstObserverHandledEventsPerPartition.Count.ShouldEqual(2);
    [Fact] void should_call_handle_on_first_observer_with_correct_event_source_id_for_first_event() => _firstObserverHandledEventsPerPartition[_firstEventSourceId][0].Partition.Value.ShouldEqual(_firstEventSourceId.Value);
    [Fact] void should_call_handle_on_first_observer_with_correct_event_source_id_for_second_event() => _firstObserverHandledEventsPerPartition[_secondEventSourceId][0].Partition.Value.ShouldEqual(_secondEventSourceId.Value);
    [Fact] void should_call_handle_on_first_observer_with_correct_event_for_first_event() => _firstObserverHandledEventsPerPartition[_firstEventSourceId][0].Events.ShouldContainOnly(_firstAppendedEvent);
    [Fact] void should_call_handle_on_first_observer_with_correct_event_for_second_event() => _firstObserverHandledEventsPerPartition[_secondEventSourceId][0].Events.ShouldContainOnly(_secondAppendedEvent);
    [Fact] void should_call_handle_on_second_observer_twice() => _secondObserverHandledEventsPerPartition.Sum(_ => _.Value.Count).ShouldEqual(2);
    [Fact] void should_call_handle_on_second_observer_with_correct_event_source_id_for_first_event() => _secondObserverHandledEventsPerPartition[_firstEventSourceId][0].Partition.Value.ShouldEqual(_firstEventSourceId.Value);
    [Fact] void should_call_handle_on_second_observer_with_correct_event_source_id_for_second_event() => _secondObserverHandledEventsPerPartition[_secondEventSourceId][0].Partition.Value.ShouldEqual(_secondEventSourceId.Value);
    [Fact] void should_call_handle_on_second_observer_with_correct_event_for_first_event() => _secondObserverHandledEventsPerPartition[_firstEventSourceId][0].Events.ShouldContainOnly(_firstAppendedEvent);
    [Fact] void should_call_handle_on_second_observer_with_correct_event_for_second_event() => _secondObserverHandledEventsPerPartition[_secondEventSourceId][0].Events.ShouldContainOnly(_secondAppendedEvent);
}
