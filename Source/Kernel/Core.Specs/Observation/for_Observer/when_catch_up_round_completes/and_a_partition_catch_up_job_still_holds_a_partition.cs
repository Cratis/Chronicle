// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Properties;
using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_catch_up_round_completes;

/// <summary>
/// The observer-wide round only owned the partitions it registered. A partition held by its own catch-up job keeps its
/// hold until that job reports back through PartitionCaughtUp (#5379).
/// </summary>
public class and_a_partition_catch_up_job_still_holds_a_partition : given.an_observer_with_skip_detection
{
    static readonly Key _roundPartition = new("A", ArrayIndexers.NoIndexers);
    static readonly Key _jobPartition = new("B", ArrayIndexers.NoIndexers);

    async Task Establish()
    {
        _stateStorage.State = _stateStorage.State with { NextEventSequenceNumber = 9UL };
        await _observer.RegisterCatchingUpPartitions([_roundPartition, _jobPartition]);
        _eventSequence.GetTailSequenceNumber().Returns((EventSequenceNumber)12UL);
        _eventSequence.GetNextSequenceNumberGreaterOrEqualTo(Arg.Any<EventSequenceNumber>(), Arg.Any<IEnumerable<EventType>>())
            .Returns(GetSequenceNumberError.NotFound);
        HasOutstandingJobs(new JobState
        {
            Id = JobId.New(),
            Status = JobStatus.Running,
            Request = new CatchUpObserverPartitionRequest(_observerKey, ObserverType.Reactor, _jobPartition, EventSequenceNumber.First, [])
        });
    }

    async Task Because() => await _observer.CaughtUp(JobId.NotSet, 12UL, 12UL);

    [Fact] void should_keep_the_partition_held_by_its_own_job() => _stateStorage.State.CatchingUpPartitions.ShouldContain(_jobPartition);
    [Fact] void should_release_the_partition_only_the_round_held() => _stateStorage.State.CatchingUpPartitions.ShouldNotContain(_roundPartition);
}
