// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans.TestKit;

namespace Cratis.Chronicle.Grains.StateMachines;

public class when_deactivating_grain : given.a_state_machine_with_well_known_states
{
    protected override Type InitialState => typeof(StateThatDoesNotSupportTransitioningFrom);

    void Establish()
    {
        // We clear this, because we don't care about the initial state transitions and state written to storage in this spec
        _ = StateMachine;  // Since the state machine is lazily created, we need an instance of it before we can clear states since it performs operations on creation that we record
        _silo.StorageStats<StateMachineForTesting, StateMachineStateForTesting>().ResetCounts();
        on_calls.Clear();

        state_that_does_not_support_transitioning.StateToReturnOnLeave = new() { Something = "Leave State" };
    }

    async Task Because() => await StateMachine.OnDeactivateAsync(new DeactivationReason(DeactivationReasonCode.None, string.Empty), CancellationToken.None);

    [Fact] void should_call_the_state_transitioned_from() => on_calls[0].Type.ShouldEqual(typeof(StateThatDoesNotSupportTransitioningFrom));
    [Fact] void should_call_on_leave_on_the_state_transitioned_from() => on_calls[0].IsEnter.ShouldBeFalse();
    [Fact] void should_write_state_returned_from_on_leave() => _stateStorage.State.ShouldEqual(state_that_does_not_support_transitioning.StateToReturnOnLeave);
}
