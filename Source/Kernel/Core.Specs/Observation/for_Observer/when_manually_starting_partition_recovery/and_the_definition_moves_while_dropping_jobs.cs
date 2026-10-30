// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_manually_starting_partition_recovery;

public class and_the_definition_moves_while_dropping_jobs : given.an_observer_with_a_failed_partition
{
    void Establish()
    {
        var blocker = for_Observer.given.stopped_retry_jobs.NeverPrepared(_observerKey, Partition);
        HasOutstandingJobs(blocker);
        _jobsManager.When(_ => _.Delete(blocker.Id)).Do(_ =>
            _definitionStorage.State = _definitionStorage.State with { EventSequenceId = (EventSequenceId)"another-sequence" });
    }

    async Task Because() => _outcome = await _observer.TryStartRecoverJobForFailedPartition(Partition);

    [Fact] void should_keep_the_existing_started_outcome() => _outcome.ShouldEqual(PartitionRecoveryOutcome.Started);
    [Fact] void should_log_that_the_observer_is_no_longer_active() => LoggedReason("ObserverNotActive").ShouldBeTrue();
    [Fact] void should_not_claim_a_scheduled_retry() => LoggedReason("RetryScheduled").ShouldBeFalse();
    [Fact] void should_not_start_a_job_under_the_old_key() => _jobsManager.DidNotReceive().Start<IRetryFailedPartition, RetryFailedPartitionRequest>(Arg.Any<RetryFailedPartitionRequest>());
}
