// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueue.when_waking_dormant_observers;

public class when_the_silo_is_stopping : given.a_queue_with_a_stored_durable_subscription
{
    int _woken;
    bool _stillDormant;

    protected override IShutdownDrainBudget? StopBudget
    {
        get
        {
            var budget = Substitute.For<IShutdownDrainBudget>();
            budget.IsStopping.Returns(true);
            budget.Token.Returns(CancellationToken.None);
            return budget;
        }
    }

    async Task Establish()
    {
        var id = IdInQueue(0, "stopping");
        Store(id, Storage.Observation.ObserverSubscriptionKind.ClientReactor);
        ObserverFor(KeyOf(id), () =>
        {
            Interlocked.Increment(ref _woken);
            return Task.CompletedTask;
        });
        await _queue.SeedDormantSubscriptions();
    }

    async Task Because()
    {
        await _queue.Enqueue([EventAt(1)]);
        await _queue.AwaitQueueDepletion(periodNum: 500);
        _stillDormant = _queue.DormantCount > 0;
    }

    [Fact] void should_not_activate_an_observer_on_a_silo_that_is_stopping() => _woken.ShouldEqual(0);
    [Fact] void should_leave_it_dormant_for_the_safety_net_of_the_next_owner() => _stillDormant.ShouldBeTrue();
}
