// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Orleans.StateMachines;

public class when_deactivating_grain : given.a_state_machine_with_well_known_states
{
    protected override Type initial_state => typeof(StateThatDoesNotSupportTransitioning);

    void Establish()
    {
        // We clear this, because we don't care about the initial state written to storage in this spec
        written_states.Clear();

        state_that_does_not_support_transitioning.StateToReturnOnLeave = new() { Something = "Leave State" };
    }

    async Task Because() => await grain.OnDeactivateAsync(new DeactivationReason(DeactivationReasonCode.None, string.Empty), CancellationToken.None);

    [Fact] void should_call_the_state_transitioned_from() => on_calls[0].Type.ShouldEqual(typeof(StateThatDoesNotSupportTransitioning));
    [Fact] void should_call_on_leave_on_the_state_transitioned_from() => on_calls[0].IsEnter.ShouldBeFalse();
    [Fact] void should_write_state_returned_from_on_leave() => written_states[0].ShouldEqual(state_that_does_not_support_transitioning.StateToReturnOnLeave);
}
