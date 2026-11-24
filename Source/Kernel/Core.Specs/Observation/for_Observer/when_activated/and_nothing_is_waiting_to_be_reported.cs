// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Alerts;
using Moq;

namespace Cratis.Chronicle.Observation.for_Observer.when_activated;

/// <summary>
/// A healthy observer reports nothing on activation and touches neither the reminder table nor its alert companion, so
/// waking thousands of dormant observers neither writes reminders nor activates thousands of companions (#5677, #6113).
/// </summary>
public class and_nothing_is_waiting_to_be_reported : given.an_observer
{
    async Task Because()
    {
        await Crash();
        await _silo.TimerRegistry.FireAllAsync();
    }

    [Fact] async Task should_not_touch_the_alert_companion() => await _observerAlerts.DidNotReceive().Reconcile(Arg.Any<ObserverAlertSnapshot>());
    [Fact] void should_not_register_the_alert_reminder() => _silo.ReminderRegistry.Mock.Verify(registry => registry.RegisterOrUpdateReminder(It.IsAny<GrainId>(), Observer.AlertReminderName, It.IsAny<TimeSpan>(), It.IsAny<TimeSpan>()), Times.Never);
    [Fact] void should_not_look_any_reminder_up() => _silo.ReminderRegistry.Mock.Verify(registry => registry.GetReminder(It.IsAny<GrainId>(), It.IsAny<string>()), Times.Never);
}
