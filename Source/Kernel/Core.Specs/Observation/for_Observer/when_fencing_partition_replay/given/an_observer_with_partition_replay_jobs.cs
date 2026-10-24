// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Monads;
using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;
using JobError = Cratis.Orleans.Storage.Jobs.JobError;

namespace Cratis.Chronicle.Observation.for_Observer.when_fencing_partition_replay.given;

public class an_observer_with_partition_replay_jobs : for_Observer.given.an_observer
{
    protected readonly Key _partition = "partition";
    protected readonly Key _otherPartition = "other-partition";
    protected readonly JobId _firstJob = JobId.New();
    protected readonly JobId _secondJob = JobId.New();
    protected IImmutableList<JobState> _jobs = ImmutableList<JobState>.Empty;

    void Establish() => _jobStorage.GetJobs<IReplayObserverPartition, JobState>()
        .Returns(_ => Task.FromResult(Catch<IImmutableList<JobState>, JobError>.Success(_jobs)));

    protected JobState ReplayJob(JobId id, Key partition, JobStatus? status = null, ObserverKey? observerKey = null) => new()
    {
        Id = id,
        Status = status ?? JobStatus.Running,
        Request = new ReplayObserverPartitionRequest(observerKey ?? _observerKey, ObserverType.Reactor, partition, EventSequenceNumber.First, 42UL, [])
    };
}
