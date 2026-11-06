// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueue.when_enqueuing.with_a_blocked_observer;

/// <summary>
/// A batch can hold any number of events, so counting batches does not bound memory. An observer whose backlog holds
/// more events than the event capacity falls back to catch-up even though it is within the batch capacity, which
/// leaves the observer it shares the queue with subscribed and still getting events.
/// </summary>
public class and_its_backlog_holds_more_events_than_the_event_capacity : given.a_blocked_observer_and_a_free_observer
{
    const int Batches = 6;
    IEnumerable<ObserverKey> _subscribedWhileBlocked;

    protected override int QueueBoundedCapacity => 1000;

    protected override int QueueBoundedEvents => 2;

    async Task Because()
    {
        for (var batch = 0; batch < Batches; batch++)
        {
            await _queue.Enqueue([EventFor("partition")]);
            await WaitTill(() => FreeObserverEventCount == batch + 1);
        }

        await WaitTill(() => _blockedObserver.ReceivedCalls().Any(call => call.GetMethodInfo().Name == nameof(IObserver.CatchUp)));
        _subscribedWhileBlocked = (await _queue.GetSubscriptions()).Select(subscription => subscription.ObserverKey).ToArray();
        _release.SetResult();
        await _queue.AwaitQueueDepletion();
    }

    [Fact] void should_spill_the_lagging_observer_to_catch_up() => _blockedObserver.Received(1).CatchUp();
    [Fact] void should_remove_only_the_lagging_observers_subscription() => _subscribedWhileBlocked.ShouldContainOnly(_freeObserverKey);
    [Fact] void should_keep_delivering_to_the_other_observer() => FreeObserverEventCount.ShouldEqual(Batches);
    [Fact] void should_not_spill_the_other_observer() => _freeObserver.DidNotReceive().CatchUp();
    [Fact] void should_not_deliver_the_discarded_backlog_to_the_lagging_observer() => _blockedObserverHandled.Count.ShouldEqual(1);
}
