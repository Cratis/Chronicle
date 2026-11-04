// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Alerts;
using Moq;

namespace Cratis.Chronicle.Observation.for_Observer.when_addressed_under_a_mismatching_key;

public class and_a_reminder_fires : given.an_observer_under_a_mismatching_key
{
    IGrainReminder _reminder;

    async Task Establish()
    {
        _reminder = await _observer.RegisterOrUpdateReminder(Observer.AlertReminderName, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
        _storageStats.ResetCounts();
        _observerAlerts.ClearReceivedCalls();
        _jobsManager.ClearReceivedCalls();
    }

    async Task Because() => await _observer.ReceiveReminder(Observer.AlertReminderName, default);

    [Fact] void should_unregister_the_reminder() => _silo.ReminderRegistry.Mock.Verify(registry => registry.UnregisterReminder(It.IsAny<GrainId>(), _reminder), Times.Once);
    [Fact] async Task should_not_reconcile_alerts() => await _observerAlerts.DidNotReceive().Reconcile(Arg.Any<ObserverAlertSnapshot>());
    [Fact] void should_not_write_state() => _storageStats.Writes.ShouldEqual(0);
    [Fact] void should_not_write_the_definition() => _definitionStorageStats.Writes.ShouldEqual(0);
    [Fact] void should_not_start_or_resume_jobs() => _jobsManager.ReceivedCalls().ShouldBeEmpty();
}
