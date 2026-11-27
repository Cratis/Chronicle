// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Observation.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_catch_up_round_completes;

public class and_the_bounded_range_contains_no_matching_events : given.an_observer_behind_a_repair_barrier
{
    async Task Because()
    {
        await _observer.CatchUp();
        await _silo.RunCatchUpContinuation();
        await _observer.HandoverRepairCaughtUp("P", 11UL, 14UL, EventSequenceNumber.Unavailable, succeeded: true);
        await _silo.RunCatchUpContinuation();
    }

    [Fact] void should_discharge_the_exhausted_range() => _stateStorage.State.PendingCatchUpRepairs!.Count.ShouldEqual(0);
    [Fact] void should_continue_at_the_frozen_cursor() => _jobsManager.Received(1).Start<ICatchUpObserver, CatchUpObserverRequest>(Arg.Is<CatchUpObserverRequest>(_ => _.FromEventSequenceNumber == (EventSequenceNumber)14UL));
    [Fact] void should_not_invent_a_partition_failure() => _failedPartitionsStorage.State.Partitions.ShouldBeEmpty();
    [Fact] void should_not_deliver_the_suffix_live() => _delivered.Count.ShouldEqual(0);
}
