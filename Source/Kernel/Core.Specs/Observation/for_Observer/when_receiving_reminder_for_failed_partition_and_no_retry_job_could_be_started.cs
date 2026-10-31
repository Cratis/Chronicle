// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Monads;
using Cratis.Orleans.Jobs;
using Moq;

namespace Cratis.Chronicle.Observation.for_Observer;

/// <summary>
/// The retry reminder is unregistered when it is received. When no retry job comes out of it, nothing else drives
/// the partition, so another retry has to be scheduled.
/// </summary>
public class when_receiving_reminder_for_failed_partition_and_no_retry_job_could_be_started : given.an_observer_with_subscription
{
    const string Partition = "SomePartition";
    FailedPartition _failedPartition;

    void Establish()
    {
        _failedPartition = new()
        {
            Partition = (Key)Partition
        };
        _failedPartitionsState.Partitions = [_failedPartition];
        _jobsManager
            .Start<IRetryFailedPartition, RetryFailedPartitionRequest>(Arg.Any<RetryFailedPartitionRequest>())
            .Returns(Task.FromResult(Result<JobId, StartJobError>.Failed(StartJobError.Unknown)));
    }

    async Task Because() => await _observer.ReceiveReminder(Partition, default);

    [Fact]
    void should_schedule_another_retry() => _silo.ReminderRegistry.Mock.Verify(
        _ => _.RegisterOrUpdateReminder(It.IsAny<GrainId>(), Observer.PartitionReminderName(_failedPartition.Id), It.IsAny<TimeSpan>(), It.IsAny<TimeSpan>()),
        Times.Once);
}
