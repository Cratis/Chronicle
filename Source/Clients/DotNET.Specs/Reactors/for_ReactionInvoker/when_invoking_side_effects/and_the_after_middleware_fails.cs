// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Reactors.for_ObserverInvoker.when_invoking_side_effects;

public class and_the_after_middleware_fails : given.a_reactor_with_recorded_side_effects
{
    void Establish() => _middlewares.AfterInvoke(Arg.Any<ReactorId>(), Arg.Any<EventContext>(), Arg.Any<object>()).Returns(_ =>
    {
        _calls.Add("after");
        throw new AfterMiddlewareFailed();
    });

    async Task Because() => _result = await Invoke(new SyncReactor());

    [Fact] void should_still_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_have_dispatched_the_side_effect_before_the_failure() => _calls.ShouldEqual(["before", "side-effect", "after"]);

    class AfterMiddlewareFailed : Exception;

#pragma warning disable ARC0002 // This directly invoked reactor intentionally declares Handle; it is not an Arc command.
    class SyncReactor : IReactor
    {
        public Effect Handle(Triggered @event) => new();
    }
}
