// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Alerts;
using Moq;

namespace Cratis.Chronicle.Observation.for_Observer.when_activated;

/// <summary>
/// A healthy observer confirms its alert level on activation without touching the reminder table, so a cold start of
/// thousands of them neither writes thousands of reminders nor leaves them overdue for the next start (#5677).
/// </summary>
public class and_nothing_is_waiting_to_be_reported : given.an_observer
{
    async Task Because()
    {
        await Crash();
        await _silo.TimerRegistry.FireAllAsync();
    }

    [Fact] async Task should_confirm_the_alert_level() => await _observerAlerts.Received(1).Reconcile(Arg.Is<ObserverAlertSnapshot>(_ => _.FailedPartitions.Count == 0 && !_.IsQuarantined));
    [Fact] void should_not_register_the_alert_reminder() => _silo.ReminderRegistry.Mock.Verify(registry => registry.RegisterOrUpdateReminder(It.IsAny<GrainId>(), Observer.AlertReminderName, It.IsAny<TimeSpan>(), It.IsAny<TimeSpan>()), Times.Never);
    [Fact] void should_not_look_any_reminder_up() => _silo.ReminderRegistry.Mock.Verify(registry => registry.GetReminder(It.IsAny<GrainId>(), It.IsAny<string>()), Times.Never);
}
