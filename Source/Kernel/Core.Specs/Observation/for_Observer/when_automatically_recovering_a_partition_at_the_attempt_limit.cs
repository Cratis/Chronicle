// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer;

public class when_automatically_recovering_a_partition_at_the_attempt_limit : given.an_exhausted_partition
{
    protected override int AttemptsInBudget => MaxRetryAttempts;

    void Establish() => FailedPartition.IsQuarantined = false;

    async Task Because()
    {
        await _observer.TryRecoverAllFailedPartitions();
        await _silo.TimerRegistry.FireAllAsync();
    }

    [Fact] void should_start_the_last_automatic_retry() => _jobsManager.Received(1)
        .Start<IRetryFailedPartition, RetryFailedPartitionRequest>(Arg.Any<RetryFailedPartitionRequest>());
    [Fact] void should_preserve_the_attempt_budget() => FailedPartition.AttemptsInCurrentBudget.ShouldEqual(MaxRetryAttempts);
    [Fact] void should_not_quarantine_before_the_retry_fails() => FailedPartition.IsQuarantined.ShouldBeFalse();
}
