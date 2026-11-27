// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Observation.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_catch_up_round_completes;

public class and_a_bounded_repair_fails : given.an_observer_behind_a_repair_barrier
{
    void Establish() => GivenHealthySuccessorPartition("Q");

    async Task Because()
    {
        await _observer.CatchUp();
        await _silo.RunCatchUpContinuation();
        await _observer.HandoverRepairCaughtUp("P", 11UL, 14UL, 11UL, succeeded: false);
        await _silo.RunCatchUpContinuation();
    }

    [Fact] void should_remove_the_repair_obligation() => _stateStorage.State.PendingCatchUpRepairs!.Count.ShouldEqual(0);
    [Fact] void should_preserve_the_ordinary_failure_position() => _failedPartitionsStorage.State.Partitions.Single(_ => _.Partition == (Key)"P").LastAttempt.SequenceNumber.ShouldEqual((EventSequenceNumber)12UL);
    [Fact] void should_release_the_barrier_at_the_frozen_cursor() => _jobsManager.Received(1).Start<ICatchUpObserver, CatchUpObserverRequest>(Arg.Is<CatchUpObserverRequest>(_ => _.FromEventSequenceNumber == (EventSequenceNumber)14UL));
    [Fact] void should_not_retry_the_failed_range_as_a_handover_repair() => _jobsManager.Received(1).Start<ICatchUpObserverPartition, CatchUpObserverPartitionRequest>(Arg.Any<CatchUpObserverPartitionRequest>());
}
