// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;
using Cratis.Orleans.Storage.Jobs;
using JobError = Cratis.Orleans.Storage.Jobs.JobError;

namespace Cratis.Chronicle.Observation.for_Observer.when_fencing_partition_replay;

public class and_a_handed_off_job_is_finalized : given.an_observer_with_partition_replay_jobs
{
    void Establish()
    {
        _stateStorage.State = _stateStorage.State with { PartitionReplayCompletions = [new(_partition, _firstJob) { LastHandledEventSequenceNumber = 42UL, LastScannedEventSequenceNumber = 48UL, HandingOff = true, HandedOff = true }] };
        _jobStorage.GetJob(_firstJob).Returns(Task.FromResult<Catch<JobState, JobError>>(JobError.NotFound));
    }

    Task Because() => Crash();

    [Fact] void should_forget_the_finalized_identity() => _stateStorage.State.PartitionReplayCompletions!.ShouldBeEmpty();
    [Fact] void should_not_repeat_the_handoff() => CheckDidNotStartCatchupJob();
}
