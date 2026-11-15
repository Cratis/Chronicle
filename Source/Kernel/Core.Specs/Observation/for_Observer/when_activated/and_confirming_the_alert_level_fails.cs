// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Alerts;
using Moq;

namespace Cratis.Chronicle.Observation.for_Observer.when_activated;

/// <summary>
/// A confirmation that does not land is retried by the watchdog while the activation lives, rather than through a
/// durable reminder that every failing activation in a cold-start storm would write (#5677).
/// </summary>
public class and_confirming_the_alert_level_fails : given.an_observer
{
    void Establish() => FailAlertReports(new TimeoutException());

    async Task Because()
    {
        await Crash();
        await _silo.TimerRegistry.FireAllAsync();
        ApplyAlertReports();
        _observerAlerts.ClearReceivedCalls();
        await _observer.RunWatchdogAsync();
    }

    [Fact] void should_not_register_the_alert_reminder() => _silo.ReminderRegistry.Mock.Verify(registry => registry.RegisterOrUpdateReminder(It.IsAny<GrainId>(), Observer.AlertReminderName, It.IsAny<TimeSpan>(), It.IsAny<TimeSpan>()), Times.Never);
    [Fact] async Task should_confirm_it_again_on_the_watchdog() => await _observerAlerts.Received(1).Reconcile(Arg.Is<ObserverAlertSnapshot>(_ => _.FailedPartitions.Count == 0 && !_.IsQuarantined));
}
