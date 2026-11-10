// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Observation.States;

namespace Cratis.Chronicle.Observation.for_Observer.when_catch_up_round_completes;

/// <summary>
/// The observer leaves <see cref="Observing"/> while the handover waits on the subscriber. The handover stops rather
/// than deliver to a subscription that is gone, leaving the position at the first event it did not hand over.
/// </summary>
public class and_the_observer_is_unsubscribed_during_the_handover : given.an_observer_handing_over_what_a_sweep_held_back
{
    async Task Because()
    {
        var handover = StartHandover();
        await _observer.Unsubscribe();
        ReleaseTheFirstHandedOverEvent();
        await handover;
    }

    [Fact] void should_not_hand_anything_over_after_the_observer_left_observing() => _delivered.SequenceEqual([15UL]).ShouldBeTrue();
    [Fact] void should_leave_the_position_at_the_first_event_not_handed_over() => _stateStorage.State.NextEventSequenceNumber.ShouldEqual((EventSequenceNumber)16UL);
    [Fact] async Task should_lower_the_hold() => (await _observer.IsPreparingCatchup()).ShouldBeFalse();
    [Fact] void should_let_go_of_what_was_held_back() => _observer.EventsHeldBackForSweep.ShouldEqual(0);
    [Fact] async Task should_stay_disconnected() => (await _observer.GetCurrentState()).ShouldBeOfExactType<Disconnected>();
}
