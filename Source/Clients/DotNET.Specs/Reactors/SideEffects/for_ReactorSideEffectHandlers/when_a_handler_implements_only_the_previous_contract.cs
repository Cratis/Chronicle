// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;

namespace Cratis.Chronicle.Reactors.SideEffects.for_ReactorSideEffectHandlers;

/// <summary>
/// The event store was added to <c>CanHandle</c> so a handler stops capturing one namespace's event type
/// registry. A handler written against the previous contract — the overload without the event store — is
/// outside this repository and cannot be updated in step, so it has to keep being asked.
/// </summary>
public class when_a_handler_implements_only_the_previous_contract : Specification
{
    readonly object _value = new();

    IEventStore _eventStore;
    HandlerOnThePreviousContract _handler;
    ReactorSideEffectHandlers _handlers;
    bool _result;

    void Establish()
    {
        _eventStore = Substitute.For<IEventStore>();
        _handler = new HandlerOnThePreviousContract();
        _handlers = new ReactorSideEffectHandlers(new KnownInstancesOf<IReactorSideEffectHandler>([_handler]));
    }

    void Because() => _result = _handlers.CanHandle(
        new ReactorContext(Events.EventContext.Empty, new object(), ReactorContextValues.Empty),
        _eventStore,
        _value);

    [Fact] void should_still_ask_the_handler() => _handler.Asked.ShouldBeTrue();
    [Fact] void should_honor_its_answer() => _result.ShouldBeTrue();

    sealed class HandlerOnThePreviousContract : IReactorSideEffectHandler
    {
        public bool Asked { get; private set; }

        public bool CanHandle(ReactorContext reactorContext, object value)
        {
            Asked = true;
            return true;
        }

        public Task<Result<ReactorSideEffectFailure>> Handle(ReactorContext reactorContext, IEventStore eventStore, object value) =>
            Task.FromResult(Result.Success<ReactorSideEffectFailure>());
    }
}
