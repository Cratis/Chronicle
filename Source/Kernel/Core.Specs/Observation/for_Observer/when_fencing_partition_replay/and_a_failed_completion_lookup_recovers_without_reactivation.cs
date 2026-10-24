// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Monads;
using Cratis.Orleans.Storage.Jobs;
using JobError = Cratis.Orleans.Storage.Jobs.JobError;

namespace Cratis.Chronicle.Observation.for_Observer.when_fencing_partition_replay;

public class and_a_failed_completion_lookup_recovers_without_reactivation : given.an_observer_with_partition_replay_jobs
{
    bool _held;

    void Establish()
    {
        _stateStorage.State.ReplayingPartitions.Add(_partition);
        EventSequenceHasNextEvent(48UL);
        _jobStorage.GetJobs<IReplayObserverPartition, JobState>()
            .Returns(Task.FromResult<Catch<IImmutableList<JobState>, JobError>>(new InvalidOperationException("Unavailable")));
    }

    async Task Because()
    {
        await _observer.PartitionReplayed(_partition, 42UL, 48UL, [], _firstJob);
        _held = _stateStorage.State.ReplayingPartitions.Contains(_partition) && _stateStorage.State.PartitionReplayCompletions!.Any();
        SetReplayJobs(ReplayJob(_firstJob));
        await RunRetryTimer();
    }

    [Fact] void should_hold_the_durable_completion_during_the_failure() => _held.ShouldBeTrue();
    [Fact] void should_release_the_fence_after_retry() => _stateStorage.State.ReplayingPartitions.ShouldNotContain(_partition);
    [Fact] void should_resume_from_the_scanned_position() => CheckStartedCatchupJob(48UL, _partition);
}
