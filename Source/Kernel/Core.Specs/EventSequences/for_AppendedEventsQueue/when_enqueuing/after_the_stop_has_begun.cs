// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueue.when_enqueuing;

/// <summary>
/// A batch committed after the stop has begun is not delivered live. Live handling moves an observer past a gap, so the
/// observer is caught up from its cursor instead of silently missing the batch.
/// </summary>
public class after_the_stop_has_begun : given.a_subscriber_with_a_stop_budget
{
    IReadOnlyList<AppendedEventsQueueObserverSubscription> _subscriptions;

    async Task Because()
    {
        Stopping = true;
        await _queue.Enqueue([EventAt(1)]);
        await _caughtUp.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        _subscriptions = await _queue.GetSubscriptions();
    }

    [Fact] async Task should_not_deliver_the_batch_live() => await _observer.DidNotReceive().Handle(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>());
    [Fact] async Task should_catch_the_observer_up() => await _observer.Received(1).CatchUp();
    [Fact] void should_take_the_observer_off_live_delivery() => _subscriptions.ShouldBeEmpty();
}
