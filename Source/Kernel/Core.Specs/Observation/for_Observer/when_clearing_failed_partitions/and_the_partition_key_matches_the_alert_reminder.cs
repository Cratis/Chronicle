// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Moq;

namespace Cratis.Chronicle.Observation.for_Observer.when_clearing_failed_partitions;

public class and_the_partition_key_matches_the_alert_reminder : given.an_observer
{
    IGrainReminder _alertReminder;
    IGrainReminder _partitionReminder;

    async Task Establish()
    {
        await _observer.PartitionFailed(Observer.AlertReminderName, 12UL, ["Failure"], "Stack");
        _alertReminder = await _observer.GetReminder(Observer.AlertReminderName);
        _partitionReminder = await _observer.GetReminder(Observer.PartitionReminderName(Observer.AlertReminderName));
    }

    async Task Because() => await _observer.ClearFailedPartitions();

    [Fact] void should_remove_the_retry_reminder() => _silo.ReminderRegistry.Mock.Verify(registry => registry.UnregisterReminder(It.IsAny<GrainId>(), _partitionReminder), Times.Once);
    [Fact] void should_keep_the_alert_reminder() => _silo.ReminderRegistry.Mock.Verify(registry => registry.UnregisterReminder(It.IsAny<GrainId>(), _alertReminder), Times.Never);
}
