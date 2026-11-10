// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Moq;

namespace Cratis.Chronicle.Observation.for_Observer.when_receiving_reminder;

/// <summary>
/// Activation no longer looks a legacy alert reminder up (#5677); one registered by an earlier version is released
/// when it next ticks instead.
/// </summary>
public class and_it_is_a_legacy_alert_reminder : given.an_observer
{
    const string LegacyReminderName = "chronicle-observer-alert-reconciliation";
    IGrainReminder _legacyReminder;

    async Task Establish()
    {
        await Crash();
        ApplyAlertReports();
        _legacyReminder = await _observer.RegisterOrUpdateReminder(LegacyReminderName, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
    }

    async Task Because() => await _observer.ReceiveReminder(LegacyReminderName, default);

    [Fact] void should_unregister_the_legacy_reminder() => _silo.ReminderRegistry.Mock.Verify(registry => registry.UnregisterReminder(It.IsAny<GrainId>(), _legacyReminder), Times.Once);
}
