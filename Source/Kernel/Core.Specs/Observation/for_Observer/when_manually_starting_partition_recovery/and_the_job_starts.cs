// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_manually_starting_partition_recovery;

public class and_the_job_starts : given.an_observer_with_a_failed_partition
{
    async Task Because() => _outcome = await _observer.TryStartRecoverJobForFailedPartition(Partition);

    [Fact] void should_report_started() => _outcome.ShouldEqual(PartitionRecoveryOutcome.Started);
    [Fact] void should_start_the_partition_recovery_job() => _jobsManager.Received(1).Start<IRetryFailedPartition, RetryFailedPartitionRequest>(
        Arg.Is<RetryFailedPartitionRequest>(_ => _.Key == _failure.Partition && _.EpisodeId == null));
    [Fact] void should_not_report_a_deferred_start() => LoggedReason("RetryScheduled").ShouldBeFalse();
    [Fact] void should_not_report_an_inactive_observer() => LoggedReason("ObserverNotActive").ShouldBeFalse();
}
