// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Reactors.SideEffects.for_ReactorSideEffectHandlers.when_the_previous_contract_is_called_directly;

/// <summary>
/// The same call against the dispatcher rather than a single handler. This is the one a consumer is most likely
/// to hold, because it is what the reactor pipeline itself uses.
/// </summary>
public class and_the_dispatcher_does_not_implement_it : Specification
{
    IReactorSideEffectHandlers _handlers;
    Exception _error;

    void Establish() => _handlers = new ReactorSideEffectHandlers(new KnownInstancesOf<IReactorSideEffectHandler>([]));

#pragma warning disable CS0618 // Deliberate: the previous contract is the thing under specification.
    void Because() => _error = Catch.Exception(() => _handlers.CanHandle(
        new ReactorContext(Events.EventContext.Empty, new object(), ReactorContextValues.Empty),
        new object()));
#pragma warning restore CS0618

    [Fact] void should_refuse_to_answer() => _error.ShouldBeOfExactType<ReactorSideEffectHandlingRequiresEventStore>();
    [Fact] void should_point_at_the_overload_that_can() => _error.Message.ShouldContain("IEventStore");
}
