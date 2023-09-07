// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Orleans.StateMachines.when_activating;

public class with_unknown_initial_state_type : GrainSpecification<StateMachineState>
{
    Exception exception;

    protected override Guid GrainId => Guid.Empty;

    protected override string GrainKeyExtension => string.Empty;

    protected override Grain GetGrainInstance() => new StateMachineWithUnknownInitialStateType();

    async Task Because() => exception = await Catch.Exception(async () => await grain.OnActivateAsync(CancellationToken.None));

    [Fact] void should_throw_invalid_type_for_state_exception() => exception.ShouldBeOfExactType<UnknownStateTypeInStateMachine>();
}
