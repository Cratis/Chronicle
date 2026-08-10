// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Reactors.SideEffects.for_ReactorSideEffectHandlers.when_the_previous_contract_is_called_directly;

/// <summary>
/// A parameterless bare-event handler has deliberately captured no event type registry. If the previous call also
/// supplies no current event store through its context, answering <see langword="false"/> would silently drop an
/// event that the correct registry may know, so the handler refuses to guess.
/// </summary>
public class and_the_handler_has_no_registry_or_event_store : Specification
{
    IReactorSideEffectHandler _handler;
    Exception _error;

    void Establish() => _handler = new EventResultHandler();

    void Because() => _error = Catch.Exception(() => _handler.CanHandle(
        new ReactorContext(Events.EventContext.Empty, new object(), ReactorContextValues.Empty),
        new object()));

    [Fact] void should_refuse_to_guess() => _error.ShouldBeOfExactType<ReactorSideEffectHandlingRequiresEventStore>();
    [Fact] void should_point_at_the_overload_that_can_answer() => _error.Message.ShouldContain("IEventStore");
}
