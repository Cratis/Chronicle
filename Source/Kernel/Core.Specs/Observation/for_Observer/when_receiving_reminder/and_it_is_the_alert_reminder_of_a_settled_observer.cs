// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Moq;

namespace Cratis.Chronicle.Observation.for_Observer.when_receiving_reminder;

/// <summary>
/// An observer with nothing to report does not need to be woken every minute (#5677). A reminder left over from an earlier
/// activation, or from a version that kept one for every observer, releases itself on its next tick.
/// </summary>
public class and_it_is_the_alert_reminder_of_a_settled_observer : given.an_observer
{
    IGrainReminder _alertReminder;

    async Task Establish()
    {
        await Crash();
        ApplyAlertReports();
        _alertReminder = await _observer.RegisterOrUpdateReminder(Observer.AlertReminderName, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
    }

    async Task Because() => await _observer.ReceiveReminder(Observer.AlertReminderName, default);

    [Fact] void should_unregister_the_alert_reminder() => _silo.ReminderRegistry.Mock.Verify(registry => registry.UnregisterReminder(It.IsAny<GrainId>(), _alertReminder), Times.Once);
}
