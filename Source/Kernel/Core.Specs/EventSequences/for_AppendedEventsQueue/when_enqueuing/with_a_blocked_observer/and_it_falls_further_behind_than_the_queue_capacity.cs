// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueue.when_enqueuing.with_a_blocked_observer;

/// <summary>
/// An observer whose backlog outgrows the queue capacity falls back to catch-up on its own: its subscription goes, so
/// no later event can move its cursor past the ones it did not get, and catch-up re-reads them from the log. The
/// observer it shares the queue with stays subscribed and keeps getting events.
/// </summary>
public class and_it_falls_further_behind_than_the_queue_capacity : given.a_blocked_observer_and_a_free_observer
{
    const int Batches = 6;
    IEnumerable<ObserverKey> _subscribedWhileBlocked;

    protected override int QueueBoundedCapacity => 2;

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
