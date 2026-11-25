// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Reactors.for_ObserverInvoker.when_invoking_side_effects;

public class and_an_asynchronous_handler_returns_a_claimed_value : given.a_reactor_with_recorded_side_effects
{
    Effect _effect;

    async Task Because()
    {
        _effect = new();
        _result = await Invoke(new AsyncReactor(_effect));
    }

    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_hand_the_awaited_value_to_the_side_effect_handlers() => _handledValue.ShouldEqual(_effect);
    [Fact] void should_run_the_middlewares_around_the_side_effect() => _calls.ShouldEqual(["before", "side-effect", "after"]);

#pragma warning disable ARC0002 // This directly invoked reactor intentionally declares Handle; it is not an Arc command.
    class AsyncReactor(Effect effect) : IReactor
    {
        public Task<Effect> Handle(Triggered @event) => Task.FromResult(effect);
    }
}
