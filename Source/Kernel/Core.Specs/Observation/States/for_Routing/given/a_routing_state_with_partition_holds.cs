// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Properties;
using Cratis.Monads;
using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;

namespace Cratis.Chronicle.Observation.States.for_Routing.given;

/// <summary>
/// A routing state entered with one partition held for catch-up and one held for replay, whose jobs are looked up in the job storage.
/// </summary>
public class a_routing_state_with_partition_holds : a_routing_state
{
    protected IJobStorage _jobStorage;
    protected Key _catchingUpPartition;
    protected Key _replayingPartition;

    void Establish()
    {
        _catchingUpPartition = new Key("catching-up", ArrayIndexers.NoIndexers);
        _replayingPartition = new Key("replaying", ArrayIndexers.NoIndexers);

        _storedState = _storedState with
        {
            NextEventSequenceNumber = 5UL,
            CatchingUpPartitions = new HashSet<Key>([_catchingUpPartition]),
            ReplayingPartitions = new HashSet<Key>([_replayingPartition])
        };

        _tailEventSequenceNumbers = _tailEventSequenceNumbers with { Tail = 20UL, TailForEventTypes = 10UL };

        _jobStorage = Substitute.For<IJobStorage>();
        _storage.GetEventStore(Arg.Any<Concepts.EventStoreName>()).GetNamespace(Arg.Any<Concepts.EventStoreNamespaceName>()).Jobs.Returns(_jobStorage);
        HasOutstandingJobs();
    }

    protected void HasOutstandingJobs(params JobState[] jobs) =>
        _jobStorage
            .GetJobs(Arg.Any<JobQuery>())
            .Returns(_ => Task.FromResult(Catch<IImmutableList<JobState>>.Success(ImmutableList.Create(jobs))));

    protected JobState PartitionCatchUpJob(Key partition, JobStatus status = JobStatus.Running) => new()
    {
        Id = JobId.New(),
        Status = status,
        Request = new CatchUpObserverPartitionRequest(_observerKey, ObserverType.Reactor, partition, EventSequenceNumber.First, [])
    };

    protected JobState PartitionReplayJob(Key partition, JobStatus status = JobStatus.Running) => new()
    {
        Id = JobId.New(),
        Status = status,
        Request = new ReplayObserverPartitionRequest(_observerKey, ObserverType.Reactor, partition, EventSequenceNumber.First, 10UL, [])
    };
}
