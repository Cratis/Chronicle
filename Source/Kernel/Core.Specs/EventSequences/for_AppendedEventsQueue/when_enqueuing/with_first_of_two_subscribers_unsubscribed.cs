// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueue.when_enqueuing;

public class with_first_of_two_subscribers_unsubscribed : given.two_subscribed_subscribers_with_an_event_type
{
    void Establish() => _queue.Unsubscribe(_firstObserverKey);

    async Task Because()
    {
        await _queue.Enqueue([_appendedEvent]);
        await _queue.AwaitQueueDepletion();
    }

    [Fact] void should_not_deliver_event_to_unsubscribed_first_observer() => _firstObserverHandledEventsPerPartition.Count.ShouldEqual(0);
    [Fact] void should_deliver_event_to_remaining_second_observer() => _secondObserverHandledEventsPerPartition.Count.ShouldEqual(1);
    [Fact] void should_deliver_correct_event_to_second_observer() => _secondObserverHandledEventsPerPartition[_eventSourceId][0].Events.ShouldContainOnly(_appendedEvent);
}
