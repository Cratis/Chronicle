// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Orleans.StateMachines.for_State.when_asking_can_transition;

public class and_it_does_not_have_state_as_allowed : Specification
{
    bool result;
    StateWithoutAllowedTransitionState state;

    void Establish() => state = new();

    async Task Because() => result = await state.CanTransitionTo<StateThatSupportsTransitioningFrom>(null!);

    [Fact] void should_return_false() => result.ShouldBeFalse();
}
