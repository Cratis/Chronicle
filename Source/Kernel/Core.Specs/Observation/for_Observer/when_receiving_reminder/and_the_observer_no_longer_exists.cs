// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Alerts;
using Moq;

namespace Cratis.Chronicle.Observation.for_Observer.when_receiving_reminder;

public class and_the_observer_no_longer_exists : given.an_absent_observer
{
    IGrainReminder _reminder;

    async Task Establish()
    {
        await Crash(resetStorageStatistics: false);
        _reminder = await _observer.RegisterOrUpdateReminder(Observer.AlertReminderName, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
    }

    async Task Because() => await _observer.ReceiveReminder(Observer.AlertReminderName, default);

    [Fact] void should_unregister_the_orphaned_reminder() => _silo.ReminderRegistry.Mock.Verify(registry => registry.UnregisterReminder(It.IsAny<GrainId>(), _reminder), Times.Once);
    [Fact] void should_not_recreate_state() => _storageStats.Writes.ShouldEqual(0);
    [Fact] async Task should_not_activate_the_alert_tracker() => await _observerAlerts.DidNotReceive().Reconcile(Arg.Any<ObserverAlertSnapshot>());
}
