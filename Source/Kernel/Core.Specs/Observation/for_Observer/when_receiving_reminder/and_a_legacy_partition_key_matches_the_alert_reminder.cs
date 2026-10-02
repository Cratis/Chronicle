// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Observation.Jobs;
using Moq;

namespace Cratis.Chronicle.Observation.for_Observer.when_receiving_reminder;

public class and_a_legacy_partition_key_matches_the_alert_reminder : given.an_observer_with_subscription
{
    const string LegacyReminderName = "chronicle-observer-alert-reconciliation";
    IGrainReminder _alertReminder;

    async Task Establish()
    {
        _failedPartitionsState.AddFailedPartition(LegacyReminderName, 12UL);
        _alertReminder = await _observer.GetReminder(Observer.AlertReminderName);
    }

    async Task Because() => await _observer.ReceiveReminder(LegacyReminderName, default);

    [Fact] async Task should_retry_the_legacy_partition() => await _jobsManager.Received(1).Start<IRetryFailedPartition, RetryFailedPartitionRequest>(Arg.Is<RetryFailedPartitionRequest>(_ => _.Key == (Key)LegacyReminderName));
    [Fact] void should_keep_the_alert_reminder() => _silo.ReminderRegistry.Mock.Verify(registry => registry.UnregisterReminder(It.IsAny<GrainId>(), _alertReminder), Times.Never);
}
