// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Reactors.SideEffects.for_ReactorSideEffectHandlers.when_the_previous_contract_is_called_directly;

/// <summary>
/// A consumer still calling the event-store-less <c>CanHandle</c> against a handler that has moved to the
/// current contract. Every shipped handler is in that position, and the call compiles - only the obsoletion
/// warning marks it - so an answer of <see langword="false"/> would lose every side effect the reactor produces
/// with nothing anywhere to say why.
/// </summary>
public class and_the_handler_does_not_implement_it : Specification
{
    IReactorSideEffectHandler _handler;
    Exception _error;

    void Establish() => _handler = new EventResultHandler();

#pragma warning disable CS0618 // Deliberate: the previous contract is the thing under specification.
    void Because() => _error = Catch.Exception(() => _handler.CanHandle(
        new ReactorContext(Events.EventContext.Empty, new object(), ReactorContextValues.Empty),
        new object()));
#pragma warning restore CS0618

    [Fact] void should_refuse_to_answer() => _error.ShouldBeOfExactType<ReactorSideEffectHandlingRequiresEventStore>();
    [Fact] void should_point_at_the_overload_that_can() => _error.Message.ShouldContain("IEventStore");
}
