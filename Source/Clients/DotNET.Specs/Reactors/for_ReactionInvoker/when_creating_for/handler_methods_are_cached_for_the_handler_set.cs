// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors.SideEffects;

namespace Cratis.Chronicle.Reactors.for_ObserverInvoker.when_creating_for;

public class handler_methods_are_cached_for_the_handler_set : Specification
{
    IReactorSideEffectHandlers _handlers;
    IEventTypes _eventTypes;

    void Establish()
    {
        _handlers = Substitute.For<IReactorSideEffectHandlers>();
        _handlers.CanHandleReturnType(typeof(Claimed)).Returns(true);
        _eventTypes = new EventTypesForSpecifications([typeof(MyEvent)]);
    }

    void Because()
    {
        ReactorInvoker.GetEventTypesFor(_eventTypes, typeof(CachedReactor), _handlers);
        ReactorInvoker.GetEventTypesFor(_eventTypes, typeof(CachedReactor), _handlers);
    }

    [Fact] void should_inspect_the_claimed_type_only_once() => _handlers.Received(1).CanHandleReturnType(typeof(Claimed));

    class CachedReactor : IReactor
    {
        public Claimed Handle(MyEvent @event) => new();
    }

    record Claimed;
}
