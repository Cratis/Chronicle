// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_catch_up_round_completes;

public class and_live_delivery_settles_before_the_conclusion_enters : given.an_observer_splitting_a_catch_up_prefix
{
    async Task Because()
    {
        await _observer.Handle("Q", [_liveEvent]);
        await _observer.CaughtUp(JobId.NotSet, 13UL, 13UL);
        await DrainRepairsAndRunWideReader();
    }

    [Fact] void should_freeze_after_the_settled_live_event() => _accepted.NextEventSequenceNumber.ShouldEqual((EventSequenceNumber)17UL);
    [Fact] void should_accept_both_prefix_owners_with_the_frozen_position() => _accepted.PendingCatchUpRepairs!.ShouldContainOnly(new PendingCatchUpRepair("P", 11UL, 17UL), new PendingCatchUpRepair("T", 15UL, 17UL));
    [Fact] void should_bound_both_repairs() => _acceptedRepairs.Select(_ => _.ToSequenceNumber).ShouldEqual([(EventSequenceNumber)16UL, (EventSequenceNumber)16UL]);
    [Fact] void should_start_the_wide_reader_after_the_live_event() => _wide!.FromEventSequenceNumber.ShouldEqual((EventSequenceNumber)17UL);
    [Fact] void should_deliver_each_event_once() => _delivered.OrderBy(_ => _).ShouldEqual(ExpectedDeliveries());

    protected virtual EventSequenceNumber[] ExpectedDeliveries() => [11UL, 15UL, 16UL, 17UL];
}
