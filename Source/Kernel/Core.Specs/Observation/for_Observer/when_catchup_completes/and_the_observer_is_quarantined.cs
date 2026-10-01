// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.States;

namespace Cratis.Chronicle.Observation.for_Observer.when_catchup_completes;

public class and_the_observer_is_quarantined : given.a_quarantined_observer
{
    async Task Establish()
    {
        _stateStorage.State = _stateStorage.State with { LastHandledEventSequenceNumber = 40UL, NextEventSequenceNumber = 41UL };
        _stateStorage.State.CatchingUpPartitions.Add("partition");
        await _observer.CatchUp();
        _jobsManager.ClearReceivedCalls();
    }

    async Task Because()
    {
        await _observer.CaughtUp(42UL);
        await RunWatchdogTicks();
    }

    [Fact] async Task should_keep_the_quarantined_state() => (await _observer.GetCurrentState()).ShouldBeOfExactType<QuarantinedObserver>();
    [Fact] void should_keep_the_persisted_quarantine() => _stateStorage.State.RunningState.ShouldEqual(ObserverRunningState.Quarantined);
    [Fact] void should_record_last_handled() => _stateStorage.State.LastHandledEventSequenceNumber.ShouldEqual((EventSequenceNumber)42UL);
    [Fact] void should_record_the_next_position() => _stateStorage.State.NextEventSequenceNumber.ShouldEqual((EventSequenceNumber)43UL);
    [Fact] void should_clear_completed_partitions() => _stateStorage.State.CatchingUpPartitions.ShouldBeEmpty();
    [Fact] async Task should_lower_preparation() => (await _observer.IsPreparingCatchup()).ShouldBeFalse();
    [Fact] void should_persist_completion() => _storageStats.Writes.ShouldEqual(1);
    [Fact] void should_not_start_catchup() => ShouldNotHaveStartedCatchup();
    [Fact] void should_not_resubscribe() => ShouldNotHaveResubscribed();
}
