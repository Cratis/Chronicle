// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Grains.Observation.Jobs;
using Orleans.Runtime;

namespace Cratis.Chronicle.Grains.Observation.for_Observer;

public class when_receiving_reminder_for_failed_partition : given.an_observer
{
    const string _partition = "SomePartition";
    FailedPartition _failedPartition;
    IGrainReminder _reminder;

    void Establish()
    {
        _failedPartition = new FailedPartition(_partition, []);
        _failedPartitionsState.Partitions.Returns([_failedPartition]);
        _failedPartitionsState.TryGet(_partition, out Arg.Any<FailedPartition>()).Returns(callInfo =>
        {
            callInfo[1] = _failedPartition;
            return true;
        });
        
        _reminder = Substitute.For<IGrainReminder>();
        _reminder.ReminderName.Returns(_partition);
    }

    async Task Because() => await _observer.ReceiveReminder(_partition, new TickStatus(DateTime.UtcNow, TimeSpan.Zero, TimeSpan.Zero));

    [Fact] void should_start_recover_job() => _jobsManager.Received(1)
        .Start<IRetryFailedPartition, RetryFailedPartitionRequest>(
            Arg.Is<RetryFailedPartitionRequest>(_ =>
                _.ObserverKey == _observerKey &&
                _.Partition == _partition));
}