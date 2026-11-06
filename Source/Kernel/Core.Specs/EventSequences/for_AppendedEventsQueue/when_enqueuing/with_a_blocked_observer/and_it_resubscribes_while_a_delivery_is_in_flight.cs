// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueue.when_enqueuing.with_a_blocked_observer;

/// <summary>
/// An observer that unsubscribes and subscribes again gets a new lane, which waits for the old lane's delivery in
/// flight, so the observer is never delivered to by two lanes at once.
/// </summary>
public class and_it_resubscribes_while_a_delivery_is_in_flight : given.a_blocked_observer_and_a_free_observer
{
    async Task Because()
    {
        await _queue.Enqueue([EventFor("partition")]);
        await WaitTill(() => !_blockedObserverHandled.IsEmpty);

        await _queue.Unsubscribe(_blockedObserverKey);
        await _queue.Subscribe(_blockedObserverKey, [_eventType]);
        await _queue.Enqueue([EventFor("partition")]);
        await WaitTill(() => FreeObserverEventCount == 2);

        _release.SetResult();
        await WaitTill(() => _blockedObserverHandled.Count == 2);
        await _queue.AwaitQueueDepletion();
    }

    [Fact] void should_never_deliver_to_the_observer_from_two_lanes_at_once() => _blockedObserverMostConcurrentDeliveries.ShouldEqual(1);
    [Fact] void should_deliver_to_the_new_lane_once_the_first_delivery_has_settled() => _blockedObserverHandled.Count.ShouldEqual(2);
}
