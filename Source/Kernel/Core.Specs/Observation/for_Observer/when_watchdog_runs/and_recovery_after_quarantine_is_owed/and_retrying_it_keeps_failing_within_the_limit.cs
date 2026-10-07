// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Observation.States;

namespace Cratis.Chronicle.Observation.for_Observer.when_watchdog_runs.and_recovery_after_quarantine_is_owed;

public class and_retrying_it_keeps_failing_within_the_limit : for_Observer.given.an_observer_with_failed_clearance_recovery
{
    void Establish()
    {
        _eventSequence.GetTailSequenceNumber().Returns(Task.FromException<EventSequenceNumber>(_recoveryFailure));
        _eventSequence.ClearReceivedCalls();
    }

    async Task Because()
    {
        for (var i = 0; i < _observersConfig.MaxCatchupRecoveryAttempts; i++)
        {
            await _observer.RunWatchdogAsync();
        }
    }

    [Fact] async Task should_remain_disconnected() => (await _observer.GetCurrentState()).ShouldBeOfExactType<Disconnected>();
    [Fact] async Task should_not_be_quarantined() => (await _observer.IsObserverQuarantined()).ShouldBeFalse();
    [Fact] async Task should_remain_subscribed() => (await _observer.IsSubscribed()).ShouldBeTrue();
    [Fact] void should_still_owe_recovery() => _observer.OwesRecoveryAfterQuarantine().ShouldBeTrue();
    [Fact] void should_retry_the_recovery_on_every_tick() => _eventSequence.Received(_observersConfig.MaxCatchupRecoveryAttempts).GetTailSequenceNumber();
}
