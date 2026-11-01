// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_completing_partition_replay;

public class and_a_catch_up_job_already_holds_the_partition : given.a_partition_replay_covering_a_failure
{
    void Establish() => HasOutstandingJobs(new JobState
    {
        Id = JobId.New(),
        Status = JobStatus.Running,
        Request = new CatchUpObserverPartitionRequest(_observerKey, ObserverType.Reactor, _partition, 51UL, [_eventType])
    });

    async Task Because() => await CompleteReplay();

    [Fact] void should_not_start_a_duplicate_successor() => CheckDidNotStartCatchupJob();
    [Fact] void should_swap_the_replay_marker_for_the_catch_up_marker() => _completionStorage.PersistedState.CatchingUpPartitions.ShouldContain(_partition);
    [Fact] void should_release_the_replay_marker() => _completionStorage.PersistedState.ReplayingPartitions.ShouldNotContain(_partition);
    [Fact] void should_resolve_the_failure_once() => _completionStorage.PersistedState.FailedPartitionCount.ShouldEqual(FailedPartitionCount.Zero);
}
