// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.States;

namespace Cratis.Chronicle.Observation.for_Observer.when_watchdog_runs.and_recovery_after_quarantine_is_owed;

public class and_retrying_it_keeps_failing_past_the_limit : for_Observer.given.an_observer_with_failed_clearance_recovery
{
    void Establish()
    {
        _eventSequence.GetTailSequenceNumber().Returns(Task.FromException<EventSequenceNumber>(_recoveryFailure));
        _eventSequence.ClearReceivedCalls();
    }

    async Task Because()
    {
        for (var i = 0; i < _observersConfig.MaxCatchupRecoveryAttempts + 1; i++)
        {
            await _observer.RunWatchdogAsync();
        }
    }

    [Fact] async Task should_be_quarantined() => (await _observer.GetCurrentState()).ShouldBeOfExactType<QuarantinedObserver>();
    [Fact] async Task should_report_being_quarantined() => (await _observer.IsObserverQuarantined()).ShouldBeTrue();
    [Fact] void should_persist_the_quarantine() => _stateStorage.State.RunningState.ShouldEqual(ObserverRunningState.Quarantined);
    [Fact] async Task should_remain_subscribed() => (await _observer.IsSubscribed()).ShouldBeTrue();
    [Fact] void should_no_longer_owe_recovery() => _observer.OwesRecoveryAfterQuarantine().ShouldBeFalse();
    [Fact] void should_stop_retrying_once_the_limit_is_exceeded() => _eventSequence.Received(_observersConfig.MaxCatchupRecoveryAttempts).GetTailSequenceNumber();
}
