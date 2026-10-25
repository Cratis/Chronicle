// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation.Jobs;
using Cratis.Monads;
using Cratis.Orleans.Jobs;
using Moq;

namespace Cratis.Chronicle.Observation.for_Observer.when_manually_starting_partition_recovery;

public class and_starting_the_job_fails : given.an_observer_with_a_failed_partition
{
    Exception? _error;

    void Establish() => _jobsManager.Start<IRetryFailedPartition, RetryFailedPartitionRequest>(Arg.Any<RetryFailedPartitionRequest>())
        .Returns(Task.FromResult(Result<JobId, StartJobError>.Failed(StartJobError.NoJobStepsToStart)));

    async Task Because() => _error = await Catch.Exception(() => _observer.TryStartRecoverJobForFailedPartition(Partition));

    [Fact] void should_report_an_error_instead_of_started() => _error.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_identify_the_partition() => _error!.Message.ShouldContain(Partition);
    [Fact] void should_not_report_a_scheduled_retry_as_the_outcome() => LoggedReason("RetryScheduled").ShouldBeFalse();

    [Fact]
    void should_preserve_the_retry_reminder() => _silo.ReminderRegistry.Mock.Verify(
        _ => _.RegisterOrUpdateReminder(It.IsAny<GrainId>(), Observer.PartitionReminderName(_failure.Id), It.IsAny<TimeSpan>(), It.IsAny<TimeSpan>()),
        Times.Once);
}
