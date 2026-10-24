// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Monads;
using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;
using JobError = Cratis.Orleans.Storage.Jobs.JobError;

namespace Cratis.Chronicle.Observation.for_Observer.when_fencing_partition_replay;

public class and_handoff_adopts_an_existing_catchup : given.an_observer_with_partition_replay_jobs
{
    void Establish()
    {
        _stateStorage.State = _stateStorage.State with { PartitionReplayCompletions = [new(_partition, _firstJob) { LastHandledEventSequenceNumber = 42UL, LastScannedEventSequenceNumber = 48UL, HandingOff = true }] };
        _jobStorage.GetJobs<ICatchUpObserverPartition, JobState>().Returns(Task.FromResult<Catch<IImmutableList<JobState>, JobError>>(ImmutableList.Create(new JobState
        {
            Id = JobId.New(),
            Status = JobStatus.Running,
            Request = new CatchUpObserverPartitionRequest(_observerKey, _definitionStorage.State.Type, _partition, 49UL, _definitionStorage.State.EventTypes)
        })));
    }

    Task Because() => Crash();

    [Fact] void should_not_start_a_duplicate_catchup() => CheckDidNotStartCatchupJob();
    [Fact] void should_adopt_partition_ownership() => _stateStorage.State.CatchingUpPartitions.ShouldContain(_partition);
    [Fact] void should_record_the_handoff() => _stateStorage.State.PartitionReplayCompletions!.Single().HandedOff.ShouldBeTrue();
}
