// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Alerts;
using Moq;

namespace Cratis.Chronicle.Observation.for_Observer.when_receiving_reminder;

/// <summary>
/// A healthy activation leaves an alert reminder from an earlier activation in place (#5677). When this activation's
/// confirmation does not land, that leftover reminder is what wakes the observer to try again, so it must stay.
/// </summary>
public class and_it_is_a_leftover_alert_reminder_while_confirmation_fails : given.an_observer
{
    IGrainReminder _leftoverReminder;

    async Task Establish()
    {
        FailAlertReports(new TimeoutException());
        await Crash();
        await _silo.TimerRegistry.FireAllAsync();
        _leftoverReminder = await _observer.RegisterOrUpdateReminder(Observer.AlertReminderName, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
        _observerAlerts.ClearReceivedCalls();
    }

    async Task Because() => await _observer.ReceiveReminder(Observer.AlertReminderName, default);

    [Fact] async Task should_try_to_confirm_the_alert_level_again() => await _observerAlerts.Received(1).Reconcile(Arg.Any<ObserverAlertSnapshot>());
    [Fact] void should_keep_the_leftover_reminder() => _silo.ReminderRegistry.Mock.Verify(registry => registry.UnregisterReminder(It.IsAny<GrainId>(), _leftoverReminder), Times.Never);
}
