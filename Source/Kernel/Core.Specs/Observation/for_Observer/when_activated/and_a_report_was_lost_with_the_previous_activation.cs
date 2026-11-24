// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Alerts;

namespace Cratis.Chronicle.Observation.for_Observer.when_activated;

/// <summary>
/// A change whose report died with the activation that made it leaves a healthy-looking observer behind. Activation
/// does not look for it; the reminder the change registered does, on its next tick.
/// </summary>
public class and_a_report_was_lost_with_the_previous_activation : given.an_observer
{
    async Task Because()
    {
        await Crash();
        await _silo.TimerRegistry.FireAllAsync();
        await _observer.ReceiveReminder(Observer.AlertReminderName, default);
    }

    [Fact] async Task should_report_the_level_when_the_reminder_ticks() => await _observerAlerts.Received(1).Reconcile(Arg.Is<ObserverAlertSnapshot>(_ => _.FailedPartitions.Count == 0 && !_.IsQuarantined));
}
