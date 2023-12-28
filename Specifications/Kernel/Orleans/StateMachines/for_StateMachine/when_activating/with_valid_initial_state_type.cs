// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Orleans.StateMachines.when_activating;

public class with_valid_initial_state_type : given.a_state_machine
{
    Exception exception;

    protected override Type initial_state => typeof(StateThatSupportsTransitioningFrom);
    protected override IEnumerable<IState<StateMachineState>> CreateStates() => new[] { new StateThatSupportsTransitioningFrom() };

    async Task Because() => exception = await Catch.Exception(async () => await state_machine.OnActivateAsync(CancellationToken.None));

    [Fact] void should_not_throw_invalid_type_for_state_exception() => exception.ShouldBeNull();
}
