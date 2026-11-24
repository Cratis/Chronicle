// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Alerts;

namespace Cratis.Chronicle.Observation.for_Observer.when_activated;

/// <summary>
/// A report that does not land stays pending: the watchdog retries it while the activation lives, rather than every
/// activation of a cold start writing a durable reminder for it (#5677).
/// </summary>
public class and_the_reminder_report_fails : given.an_observer
{
    async Task Because()
    {
        await Crash();
        FailAlertReports(new TimeoutException());
        await _observer.ReceiveReminder(Observer.AlertReminderName, default);
        ApplyAlertReports();
        _observerAlerts.ClearReceivedCalls();
        await _observer.RunWatchdogAsync();
    }

    [Fact] async Task should_report_it_again_on_the_watchdog() => await _observerAlerts.Received(1).Reconcile(Arg.Is<ObserverAlertSnapshot>(_ => _.FailedPartitions.Count == 0 && !_.IsQuarantined));
}
