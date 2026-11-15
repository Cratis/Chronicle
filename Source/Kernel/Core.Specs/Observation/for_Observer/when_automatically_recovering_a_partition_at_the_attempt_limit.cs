// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer;

public class when_automatically_recovering_a_partition_at_the_attempt_limit : given.a_quarantined_partition
{
    void Establish()
    {
        FailedPartition.AttemptsInCurrentBudget = MaxRetryAttempts;
        FailedPartition.IsQuarantined = false;
    }

    async Task Because()
    {
        await _observer.TryRecoverAllFailedPartitions();
        await _silo.TimerRegistry.FireAllAsync();
    }

    [Fact] void should_not_start_recovery() => _jobsManager.DidNotReceive()
        .Start<IRetryFailedPartition, RetryFailedPartitionRequest>(Arg.Any<RetryFailedPartitionRequest>());
    [Fact] void should_preserve_the_attempt_budget() => FailedPartition.AttemptsInCurrentBudget.ShouldEqual(MaxRetryAttempts);
}
