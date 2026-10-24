// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation.Jobs;
using Cratis.Monads;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_fencing_partition_replay;

public class and_starting_the_job_returns_an_error_while_the_job_runs : given.an_observer_with_partition_replay_jobs
{
    void Establish()
    {
        SetReplayJobs(ReplayJob(_firstJob));
        _jobsManager.Start<IReplayObserverPartition, ReplayObserverPartitionRequest>(Arg.Any<ReplayObserverPartitionRequest>())
            .Returns(Result<JobId, StartJobError>.Failed(StartJobError.FailedStartingSomeJobSteps));
    }

    Task Because() => _observer.ReplayPartition(_partition);

    [Fact] void should_leave_the_marker_with_the_running_job() => _stateStorage.State.ReplayingPartitions.ShouldContain(_partition);
    [Fact] async Task should_keep_the_partition_fenced() => (await _observer.IsPartitionFenced(_partition, _secondJob)).ShouldBeTrue();
    [Fact] async Task should_not_start_a_catch_up() => await _jobsManager.DidNotReceive().Start<ICatchUpObserverPartition, CatchUpObserverPartitionRequest>(Arg.Any<CatchUpObserverPartitionRequest>());
}
