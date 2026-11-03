// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Observation;
using Orleans.TestKit;

namespace Cratis.Chronicle.Projections.for_ProjectionsManager;

/// <summary>
/// Membership-triggered repair shares host admission and does not reconcile away pending revival.
/// </summary>
public class when_two_event_stores_reconcile_after_a_silo_departure : given.two_projections_managers_sharing_admission
{
    bool _firstInstalled;
    bool _secondInstalled;

    async Task Establish()
    {
        await _grain.Register(_firstDefinitions);
        await _secondGrain.Register(_secondDefinitions);
        await _silo.TimerRegistry.FireAllAsync();
        await _secondSilo.TimerRegistry.FireAllAsync();
        _secondObserver.IsSubscriptionEstablished().Returns(false);
        _secondObserver.GetState().Returns(new ObserverState { AlertDisposition = AlertDisposition.Retired });
        await _secondGrain.Register(_secondDefinitions);
        _observerGrain.ClearReceivedCalls();
        _secondObserver.ClearReceivedCalls();
        _observerGrain.IsSubscriptionEstablished().Returns(_ => _firstInstalled);
        _secondObserver.IsSubscriptionEstablished().Returns(_ => _secondInstalled);
        _observerGrain.ReconcileSubscription<IProjectionObserverSubscriber>(default, default!, default!, default).ReturnsForAnyArgs(_ =>
        {
            _firstInstalled = true;
            return Task.CompletedTask;
        });
        _secondObserver.Subscribe<IProjectionObserverSubscriber>(default, default!, default!).ReturnsForAnyArgs(_ =>
        {
            _secondInstalled = true;
            return Task.CompletedTask;
        });
        HoldBuilds();
        SiloChanged(_otherSilo, SiloStatus.Dead);
    }

    async Task Because()
    {
        var firstRepair = FireRepair(_silo);
        await _eightEntered.Task.WaitAsync(TimeSpan.FromSeconds(30), TimeProvider.System);
        var secondRepair = FireRepair(_secondSilo);
        _release.TrySetResult();
        await Task.WhenAll(firstRepair, secondRepair);
        await _silo.TimerRegistry.FireAllAsync();
        await _secondSilo.TimerRegistry.FireAllAsync();
    }

    static async Task FireRepair(TestKitSilo silo)
    {
        await silo.TimerRegistry.FireAllAsync();
        await silo.TimerRegistry.FireAllAsync();
    }

    [Fact] void should_bound_combined_repair_builds() => _peak.ShouldEqual(8);
    [Fact] async Task should_reconcile_the_first_store() => await _observerGrain.ReceivedWithAnyArgs(12).ReconcileSubscription<IProjectionObserverSubscriber>(default, default!, default!, default);
    [Fact] async Task should_freshly_revive_the_pending_store() => await _secondObserver.Received(12).Subscribe<IProjectionObserverSubscriber>(ObserverType.Projection, Arg.Any<IEnumerable<Concepts.Events.EventType>>(), Arg.Any<SiloAddress>(), Arg.Any<object?>(), Arg.Any<bool>(), reactivateRetired: true);
    [Fact] async Task should_not_reconcile_pending_revival() => await _secondObserver.DidNotReceiveWithAnyArgs().ReconcileSubscription<IProjectionObserverSubscriber>(default, default!, default!, default);
}
