// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Reactors.SideEffects;
using Cratis.Monads;

namespace Cratis.Chronicle.Reactors.for_ObserverInvoker.when_invoking_side_effects;

public class and_the_side_effect_handlers_fail : given.a_reactor_with_recorded_side_effects
{
    ReactorSideEffectFailure _failure;

    void Establish()
    {
        _failure = new([]);
        _handlers.Handle(Arg.Any<ReactorContext>(), Arg.Any<IEventStore>(), Arg.Any<object>())
            .Returns(Task.FromResult<Result<ReactorSideEffectFailure>>(Result.Failed(_failure)));
    }

    async Task Because() => _result = await Invoke(new SyncReactor());

    [Fact] void should_fail() => _result.IsFailed.ShouldBeTrue();
    [Fact] void should_carry_the_side_effect_failure() => _result.SideEffectFailure.ShouldEqual(_failure);
    [Fact] void should_not_report_an_exception() => _result.ExceptionResult.TryGetException(out _).ShouldBeFalse();
    [Fact] void should_still_run_the_after_middleware() => _calls.ShouldEqual(["before", "after"]);

#pragma warning disable ARC0002 // This directly invoked reactor intentionally declares Handle; it is not an Arc command.
    class SyncReactor : IReactor
    {
        public Effect Handle(Triggered @event) => new();
    }
}
