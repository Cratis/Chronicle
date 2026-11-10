// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Observation.States;

namespace Cratis.Chronicle.Observation.for_Observer.when_catch_up_round_completes;

/// <summary>
/// The report interleaves with itself: arriving again while the first is handing over what the sweep held back, it
/// changes nothing, so the hold is not lowered and the observer not routed under the handover.
/// </summary>
public class and_the_same_round_is_reported_again_during_the_handover : given.an_observer_handing_over_what_a_sweep_held_back
{
    bool _againReturnedDuringHandover;
    bool _stillHoldingDuringHandover;

    async Task Because()
    {
        var handover = StartHandover();
        var again = StartHandover();
        _againReturnedDuringHandover = again.IsCompleted && !handover.IsCompleted;
        _stillHoldingDuringHandover = await _observer.IsPreparingCatchup();
        ReleaseTheFirstHandedOverEvent();
        await handover;
        await again;
    }

    [Fact] void should_return_the_repeated_report_without_waiting_for_the_handover() => _againReturnedDuringHandover.ShouldBeTrue();
    [Fact] void should_keep_holding_while_handing_over() => _stillHoldingDuringHandover.ShouldBeTrue();
    [Fact] void should_deliver_everything_above_what_the_sweep_read_exactly_once() => _delivered.SequenceEqual([15UL, 16UL, 17UL]).ShouldBeTrue();
    [Fact] void should_move_past_everything_delivered() => _stateStorage.State.NextEventSequenceNumber.ShouldEqual((EventSequenceNumber)18UL);
    [Fact] async Task should_be_back_on_live_delivery() => (await _observer.IsPreparingCatchup()).ShouldBeFalse();
    [Fact] void should_let_go_of_what_was_held_back() => _observer.EventsHeldBackForSweep.ShouldEqual(0);
    [Fact] async Task should_be_observing() => (await _observer.GetCurrentState()).ShouldBeOfExactType<Observing>();
    [Fact] void should_not_start_another_catch_up() => _jobsManager.Received(1).Start<ICatchUpObserver, CatchUpObserverRequest>(Arg.Any<CatchUpObserverRequest>());
}
