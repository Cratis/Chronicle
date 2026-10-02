// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Alerts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;

namespace Cratis.Chronicle.Projections.for_ProjectionsManager.when_activated;

public class and_a_retired_observer_has_a_retained_definition : given.a_manager_recovering_a_retired_observer
{
    async Task Because()
    {
        await _managerSilo.TimerRegistry.FireAllAsync();
        await _manager.Register([]);
        await _managerSilo.TimerRegistry.FireAllAsync();
        await ReportAlerts();
    }

    [Fact] async Task should_recheck_skipped_subscriptions_on_the_repair_pass() => await _managedObserver.Received(2).Subscribe<IProjectionObserverSubscriber>(ObserverType.Projection, Arg.Any<IEnumerable<EventType>>(), Arg.Any<SiloAddress>(), reactivateRetired: false);
    [Fact] async Task should_not_resubscribe_the_observer() => (await _observer.IsSubscribed()).ShouldBeFalse();
    [Fact] void should_keep_retirement_committed() => _stateStorage.State.AlertDisposition.ShouldEqual(AlertDisposition.Retired);
    [Fact] void should_keep_the_retired_lifecycle() => _stateStorage.State.AlertLifecycleId.ShouldEqual(_retiredLifecycle);
    [Fact] void should_retain_pending_replay() => _stateStorage.State.IsReplaying.ShouldBeTrue();
    [Fact] async Task should_not_start_replay() => await _jobsManager.DidNotReceive().Start<IReplayObserver, ReplayObserverRequest>(Arg.Any<ReplayObserverRequest>());
    [Fact] async Task should_not_reopen_quarantine_alerts() => await _observerAlerts.DidNotReceive().Reconcile(Arg.Is<ObserverAlertSnapshot>(snapshot => snapshot.Disposition == AlertDisposition.Active));
}
