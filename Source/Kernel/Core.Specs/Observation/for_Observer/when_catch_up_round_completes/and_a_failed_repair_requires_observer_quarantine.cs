// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Configuration;

namespace Cratis.Chronicle.Observation.for_Observer.when_catch_up_round_completes;

public class and_a_failed_repair_requires_observer_quarantine : given.an_observer_behind_a_repair_barrier
{
    bool _jobCallsDuringAcknowledgement;
    ObserverRunningState _stateAfterAcknowledgement;

    protected override Observers CreateObserversConfig() => new() { WriteCoalescingInterval = 0, QuarantineOnFailedPartitionCount = 1 };

    async Task Because()
    {
        await _observer.CatchUp();
        await _silo.RunCatchUpContinuation();
        _jobsManager.ClearReceivedCalls();
        await _observer.HandoverRepairCaughtUp("P", 11UL, 14UL, 11UL, succeeded: false).WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        _jobCallsDuringAcknowledgement = _jobsManager.ReceivedCalls().Any();
        _stateAfterAcknowledgement = _stateStorage.State.RunningState;
        await _silo.RunCatchUpContinuation();
    }

    [Fact] void should_make_no_job_calls_inside_the_acknowledgement() => _jobCallsDuringAcknowledgement.ShouldBeFalse();
    [Fact] void should_not_enter_quarantine_inside_the_acknowledgement() => _stateAfterAcknowledgement.ShouldEqual(ObserverRunningState.Active);
    [Fact] void should_durably_discharge_the_failed_repair() => _stateStorage.State.PendingCatchUpRepairs!.Count.ShouldEqual(0);
    [Fact] void should_apply_the_ordinary_quarantine_policy_in_the_continuation() => _stateStorage.State.RunningState.ShouldEqual(ObserverRunningState.Quarantined);
}
