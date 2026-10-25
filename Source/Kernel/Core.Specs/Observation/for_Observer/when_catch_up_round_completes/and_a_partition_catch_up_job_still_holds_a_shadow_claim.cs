// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Storage.Observation;
using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_catch_up_round_completes;

/// <summary>
/// Shadow claims follow the partial release (#5379): the round hands over its observer-wide claim, a partition held by its
/// own catch-up job keeps a partition claim, and the partition only the round held has none.
/// </summary>
public class and_a_partition_catch_up_job_still_holds_a_shadow_claim : given.an_observer_with_skip_detection
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

    [Fact] void should_hand_over_the_observer_wide_claim() => _stateStorage.State.ObserverWideClaim.ShouldBeNull();
    [Fact] void should_keep_a_catch_up_claim_for_the_partition_held_by_its_own_job() =>
        _stateStorage.State.PartitionClaims!.Any(_ => _.Partition == _jobPartition && _.Holder == ClaimHolderKind.PartitionCatchUp).ShouldBeTrue();
    [Fact] void should_have_no_claim_for_the_partition_only_the_round_held() =>
        _stateStorage.State.PartitionClaims!.Any(_ => _.Partition == _roundPartition).ShouldBeFalse();
    [Fact] void should_not_measure_the_partial_release_as_divergence() => _metrics.For("chronicle-observer-claim-divergences").ShouldBeEmpty();
}
