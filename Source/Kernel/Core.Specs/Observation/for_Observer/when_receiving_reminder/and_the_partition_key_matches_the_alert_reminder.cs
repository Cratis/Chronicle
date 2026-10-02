// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Observation.Jobs;
using Moq;

namespace Cratis.Chronicle.Observation.for_Observer.when_receiving_reminder;

public class and_the_partition_key_matches_the_alert_reminder : given.an_observer_with_subscription
{
    IGrainReminder _alertReminder;

    async Task Establish()
    {
        _alertReminder = await _observer.GetReminder(Observer.AlertReminderName);
        await _observer.PartitionFailed(Observer.AlertReminderName, 12UL, ["Failure"], "Stack");
    }

    async Task Because() => await _observer.ReceiveReminder(Observer.PartitionReminderName(Observer.AlertReminderName), default);

    [Fact] async Task should_retry_the_partition() => await _jobsManager.Received(1).Start<IRetryFailedPartition, RetryFailedPartitionRequest>(Arg.Is<RetryFailedPartitionRequest>(_ => _.Key == (Key)Observer.AlertReminderName));
    [Fact] void should_not_unregister_the_alert_reminder() => _silo.ReminderRegistry.Mock.Verify(registry => registry.UnregisterReminder(It.IsAny<GrainId>(), _alertReminder), Times.Never);
    [Fact] void should_register_a_separate_retry_reminder() => _silo.ReminderRegistry.Mock.Verify(registry => registry.RegisterOrUpdateReminder(It.IsAny<GrainId>(), Observer.PartitionReminderName(Observer.AlertReminderName), TimeSpan.FromSeconds(2), TimeSpan.FromMinutes(1)), Times.Once);
}
