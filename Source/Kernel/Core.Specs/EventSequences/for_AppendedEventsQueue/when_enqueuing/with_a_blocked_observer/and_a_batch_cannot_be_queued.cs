// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueue.when_enqueuing.with_a_blocked_observer;

/// <summary>
/// When a batch cannot be queued, only the observers it was meant for fall back to catch-up. An observer subscribed to
/// other event types never needed the batch, so it keeps its subscription and is not sent to catch-up.
/// </summary>
public class and_a_batch_cannot_be_queued : given.a_blocked_observer_and_a_free_observer
{
    IEnumerable<ObserverKey> _subscribedAfterwards;

    async Task Establish() => await _queue.Subscribe(_freeObserverKey, [new EventType("Some other event", 1)]);

    async Task Because()
    {
        _queue.SpillSubscribersMatchingToCatchup([EventFor("partition")]);
        await WaitTill(() => _blockedObserver.ReceivedCalls().Any(call => call.GetMethodInfo().Name == nameof(IObserver.CatchUp)));
        _subscribedAfterwards = (await _queue.GetSubscriptions()).Select(subscription => subscription.ObserverKey).ToArray();
        _release.SetResult();
    }

    [Fact] void should_spill_the_observer_the_batch_was_meant_for() => _blockedObserver.Received(1).CatchUp();
    [Fact] void should_keep_only_the_other_observer_subscribed() => _subscribedAfterwards.ShouldContainOnly(_freeObserverKey);
    [Fact] void should_not_spill_the_observer_the_batch_was_not_meant_for() => _freeObserver.DidNotReceive().CatchUp();
}
