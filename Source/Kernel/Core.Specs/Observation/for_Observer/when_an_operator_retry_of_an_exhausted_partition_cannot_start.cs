// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Monads;

namespace Cratis.Chronicle.Observation.for_Observer;

public class when_an_operator_retry_of_an_exhausted_partition_cannot_start : given.a_quarantined_partition
{
    Exception _error;

    void Establish() => _jobsManager.Start<IRetryFailedPartition, RetryFailedPartitionRequest>(Arg.Any<RetryFailedPartitionRequest>())
        .Returns(Task.FromResult(Result<JobId, StartJobError>.Failed(StartJobError.NoJobStepsToStart)));

    async Task Because() => _error = await Catch.Exception(() => _observer.TryStartRecoverJobForFailedPartition((Key)Partition));

    [Fact] void should_report_a_failure() => _error.ShouldBeOfExactType<FailedPartitionRecoveryCouldNotStart>();
    [Fact] void should_restore_the_quarantine() => FailedPartition.IsQuarantined.ShouldBeTrue();
    [Fact] void should_preserve_the_attempt_budget() => FailedPartition.AttemptsInCurrentBudget.ShouldEqual(3);
}
