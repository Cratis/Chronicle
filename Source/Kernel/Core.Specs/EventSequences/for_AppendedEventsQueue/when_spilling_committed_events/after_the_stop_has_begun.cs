// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueue.when_spilling_committed_events;

public class after_the_stop_has_begun : given.a_subscriber_with_a_stop_budget
{
    IReadOnlyList<AppendedEventsQueueObserverSubscription> _subscriptions;

    async Task Because()
    {
        Stopping = true;
        await _queue.SpillMatchingToCatchup([EventAt(7)]);
        await _caughtUp.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        _subscriptions = await _queue.GetSubscriptions();
    }

    [Fact] async Task should_catch_the_observer_up() => await _observer.Received(1).CatchUp();
    [Fact] void should_take_the_observer_off_live_delivery() => _subscriptions.ShouldBeEmpty();
}
