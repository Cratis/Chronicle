// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;

namespace Cratis.Chronicle.Reactors.SideEffects.for_ReactorSideEffectHandlers;

/// <summary>
/// Both <c>CanHandle</c> overloads carry a default, so a handler can now be written that implements neither -
/// which the previous contract made impossible, because the single overload was abstract. Such a handler has no
/// answer to give, and answering for it either way is wrong: it is told so instead of being quietly skipped.
/// </summary>
public class when_a_handler_implements_neither_contract : Specification
{
    readonly object _value = new();

    IEventStore _eventStore;
    ReactorSideEffectHandlers _handlers;
    Exception _error;

    void Establish()
    {
        _eventStore = Substitute.For<IEventStore>();
        _handlers = new ReactorSideEffectHandlers(new KnownInstancesOf<IReactorSideEffectHandler>([new HandlerOnNoContract()]));
    }

    void Because() => _error = Specifications.Catch.Exception(() => _handlers.CanHandle(
        new ReactorContext(Events.EventContext.Empty, new object(), ReactorContextValues.Empty),
        _eventStore,
        _value));

    [Fact] void should_refuse_to_answer() => _error.ShouldBeOfExactType<ReactorSideEffectHandlingRequiresEventStore>();
    [Fact] void should_name_the_handler_that_cannot_answer() => _error.Message.ShouldContain(typeof(HandlerOnNoContract).FullName!);

    sealed class HandlerOnNoContract : IReactorSideEffectHandler
    {
        public Task<Result<ReactorSideEffectFailure>> Handle(ReactorContext reactorContext, IEventStore eventStore, object value) =>
            Task.FromResult(Result.Success<ReactorSideEffectFailure>());
    }
}
