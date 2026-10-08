// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation.States;

namespace Cratis.Chronicle.Observation.for_Observer;

public class when_clearing_quarantine_with_no_event_types : given.an_observer_with_subscription
{
    bool _owedRecoveryBeforeWatchdog;

    async Task Establish()
    {
        _observer.SetSubscription(subscription with { EventTypes = [] });
        await _observer.TransitionTo<QuarantinedObserver>();
        (await _observer.IsObserverQuarantined()).ShouldBeTrue();
    }

    async Task Because()
    {
        await _observer.ClearObserverQuarantine();
        _owedRecoveryBeforeWatchdog = _observer.OwesRecoveryAfterQuarantine();
        for (var tick = 0; tick <= _observersConfig.MaxCatchupRecoveryAttempts; tick++)
        {
            await _observer.RunWatchdogAsync();
        }
    }

    [Fact] void should_settle_recovery_before_the_watchdog() => _owedRecoveryBeforeWatchdog.ShouldBeFalse();
    [Fact] async Task should_remain_disconnected() => (await _observer.GetCurrentState()).ShouldBeOfExactType<Disconnected>();
    [Fact] async Task should_not_be_quarantined_again() => (await _observer.IsObserverQuarantined()).ShouldBeFalse();
    [Fact] async Task should_retain_the_subscription() => (await _observer.IsSubscribed()).ShouldBeTrue();
}
