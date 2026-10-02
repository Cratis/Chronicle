// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Alerts;
using Moq;

namespace Cratis.Chronicle.Observation.for_Observer.when_receiving_reminder;

public class and_it_is_the_alert_reminder : given.an_observer
{
    async Task Establish()
    {
        await Crash();
        _observerAlerts.ClearReceivedCalls();
    }

    async Task Because() => await _observer.ReceiveReminder(Observer.AlertReminderName, default);

    [Fact] async Task should_report_the_activation_level() => await _observerAlerts.Received(1).Reconcile(Arg.Is<ObserverAlertSnapshot>(_ => _.FailedPartitions.Count == 0));
    [Fact] void should_not_unregister_the_lifetime_reminder() => _silo.ReminderRegistry.Mock.Verify(registry => registry.UnregisterReminder(It.IsAny<GrainId>(), It.IsAny<IGrainReminder>()), Times.Never);
}
