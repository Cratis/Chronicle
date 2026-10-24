// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Monads;
using Cratis.Orleans.Storage.Jobs;
using JobError = Cratis.Orleans.Storage.Jobs.JobError;

namespace Cratis.Chronicle.Observation.for_Observer.when_fencing_partition_replay;

public class and_a_recorded_completion_survives_reactivation : given.an_observer_with_partition_replay_jobs
{
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
        SetReplayJobs(ReplayJob(_firstJob));
        await Crash();
    }

    [Fact] void should_exclude_the_recorded_job_even_while_running() => _stateStorage.State.ReplayingPartitions.ShouldNotContain(_partition);
    [Fact] void should_hand_off_from_the_durable_scanned_position() => CheckStartedCatchupJob(48UL, _partition);
    [Fact] void should_retain_the_concluded_identity_until_finalized() => _stateStorage.State.PartitionReplayCompletions!.Single().HandedOff.ShouldBeTrue();
}
