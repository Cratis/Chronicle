// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Alerts;
using Moq;

namespace Cratis.Chronicle.Observation.for_Observer.when_activated;

public class and_the_namespace_has_no_observer : given.an_absent_observer
{
    async Task Because()
    {
        await Crash(resetStorageStatistics: false);
        await _observer.RunWatchdogAsync();
        await ReportAlerts();
    }

    [Fact] void should_not_write_state() => _storageStats.Writes.ShouldEqual(0);
    [Fact] void should_not_allocate_a_lifecycle() => _stateStorage.State.AlertLifecycleId.ShouldEqual(Guid.Empty);
    [Fact] void should_not_register_a_reminder() => _silo.ReminderRegistry.Mock.Verify(registry => registry.RegisterOrUpdateReminder(It.IsAny<GrainId>(), It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<TimeSpan>()), Times.Never);
    [Fact] async Task should_not_activate_the_alert_tracker() => await _observerAlerts.DidNotReceive().Reconcile(Arg.Any<ObserverAlertSnapshot>());
}
