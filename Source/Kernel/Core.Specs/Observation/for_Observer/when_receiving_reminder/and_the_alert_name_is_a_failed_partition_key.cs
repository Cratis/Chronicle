// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_receiving_reminder;

public class and_the_alert_name_is_a_failed_partition_key : given.an_observer_with_subscription
{
    async Task Establish()
    {
        await _observer.PartitionFailed(Observer.AlertReminderName, 12UL, ["Failure"], "Stack");
        _jobsManager.ClearReceivedCalls();
    }

    async Task Because() => await _observer.ReceiveReminder(Observer.AlertReminderName, default);

    [Fact] async Task should_leave_retry_timing_to_the_partition_reminder() => await _jobsManager.DidNotReceive().Start<IRetryFailedPartition, RetryFailedPartitionRequest>(Arg.Any<RetryFailedPartitionRequest>());
}
