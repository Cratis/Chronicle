// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Reactors.SideEffects;

namespace Cratis.Chronicle.Reactors.for_ObserverInvoker.when_invoking_side_effects;

public class and_the_returned_value_cannot_be_handled : given.a_reactor_with_recorded_side_effects
{
    Exception _exception;

    void Establish() => _handlers.CanHandle(Arg.Any<ReactorContext>(), Arg.Any<IEventStore>(), Arg.Any<object>()).Returns(false);

    async Task Because()
    {
        _result = await Invoke(new SyncReactor());
        _result.ExceptionResult.TryGetException(out _exception);
    }

    [Fact] void should_fail() => _result.IsFailed.ShouldBeTrue();
    [Fact] void should_report_the_unhandled_return_value() => _exception.ShouldBeOfExactType<UnhandledReactorReturnValue>();
    [Fact] void should_not_dispatch_to_the_side_effect_handlers() => _calls.ShouldEqual(["before", "after"]);

#pragma warning disable ARC0002 // This directly invoked reactor intentionally declares Handle; it is not an Arc command.
    class SyncReactor : IReactor
    {
        public Effect Handle(Triggered @event) => new();
    }
}
