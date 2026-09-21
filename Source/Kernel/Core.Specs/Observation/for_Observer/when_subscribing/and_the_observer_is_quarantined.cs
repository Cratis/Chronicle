// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.for_Observer.when_watchdog_runs.given;
using Cratis.Chronicle.Observation.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_subscribing;

/// <summary>
/// A fresh subscription is the changed world quarantine was waiting for: the client reconnected,
/// which in production is a redeploy or restart - the very action an operator takes to fix things.
/// Before this behavior existed, re-subscription recorded the new subscription but left the observer
/// quarantined, making quarantine terminal in practice: nothing on the client's side could ever
/// revive it, and it stayed dead across every subsequent deploy.
/// </summary>
public class and_the_observer_is_quarantined : an_observer_behind_on_a_relevant_event
{
    async Task Establish()
    {
        await _observer.CatchUp();
        for (var i = 0; i < _observersConfig.MaxCatchupRecoveryAttempts + 1; i++)
        {
            await _observer.RunWatchdogAsync();
        }

        _jobsManager.ClearReceivedCalls();
    }

    async Task Because() =>
        await _observer.Subscribe<IClientOwnedObserverSubscriber>(
            ObserverType.Reactor,
            [event_type],
            SiloAddress.Zero,
            _connectedClient);

    [Fact] void should_leave_quarantine() => _stateStorage.State.RunningState.ShouldNotEqual(ObserverRunningState.Quarantined);

    [Fact] void should_drive_catch_up_for_the_gap_again() => _jobsManager
        .Received()
        .Start<ICatchUpObserver, CatchUpObserverRequest>(Arg.Any<CatchUpObserverRequest>());
}
