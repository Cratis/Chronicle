// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Grains.Observation.Jobs;

namespace Cratis.Chronicle.Grains.Observation.for_Observer;

public class when_receiving_reminder_for_failed_partition : given.an_observer_with_subscription
{
    const string _partition = "SomePartition";
    FailedPartition _failedPartition;
    IGrainReminder _reminder;

    void Establish()
    {
        _failedPartition = new()
        {
            Partition = (Key)_partition
        };
        _failedPartitionsState.Partitions = [_failedPartition];
        _reminder = Substitute.For<IGrainReminder>();
        _reminder.ReminderName.Returns(_partition);
    }

    async Task Because() => await _observer.ReceiveReminder(_partition, default);

    [Fact]
    void should_start_recover_job() => _jobsManager.Received(1)
        .Start<IRetryFailedPartition, RetryFailedPartitionRequest>(
            Arg.Is<RetryFailedPartitionRequest>(_ =>
                _.ObserverKey == _observerKey &&
                _.Key == (Key)_partition));
}
