// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Chronicle.Reactors.for_ObserverInvoker.when_invoking_side_effects;

public class and_the_handler_throws : given.a_reactor_with_recorded_side_effects
{
    Exception _exception;

    async Task Because()
    {
        _result = await Invoke(new ThrowingReactor());
        _result.ExceptionResult.TryGetException(out _exception);
    }

    [Fact] void should_fail() => _result.IsFailed.ShouldBeTrue();
    [Fact] void should_carry_the_handler_exception_wrapped_by_reflection() => _exception.ShouldBeOfExactType<TargetInvocationException>();
    [Fact] void should_wrap_the_original_handler_exception() => _exception.InnerException.Message.ShouldEqual("boom");
    [Fact] void should_not_dispatch_to_the_side_effect_handlers() => _calls.ShouldEqual(["before", "after"]);

    class HandlerBoom : Exception
    {
        public override string Message => "boom";
    }

#pragma warning disable ARC0002 // This directly invoked reactor intentionally declares Handle; it is not an Arc command.
    class ThrowingReactor : IReactor
    {
        public Effect Handle(Triggered @event) => throw new HandlerBoom();
    }
}
