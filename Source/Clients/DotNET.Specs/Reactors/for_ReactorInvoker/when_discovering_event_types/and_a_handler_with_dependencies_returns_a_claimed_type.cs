// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors.SideEffects;

namespace Cratis.Chronicle.Reactors.for_ReactorInvoker.when_discovering_event_types;

public class and_a_handler_with_dependencies_returns_a_claimed_type : Specification
{
    IEventTypes _eventTypes;
    IReactorSideEffectHandlers _handlers;
    Exception _error;
    IEnumerable<EventType> _result;

    void Establish()
    {
        _eventTypes = new EventTypesForSpecifications([typeof(MyEvent)]);
        _handlers = Substitute.For<IReactorSideEffectHandlers>();
        _handlers.CanHandleReturnType(typeof(Claimed)).Returns(true);
    }

    void Because() => _error = Catch.Exception(() => _result = ReactorInvoker.GetEventTypesFor(_eventTypes, typeof(ClaimedReactor), _handlers));

    [Fact] void should_not_fail() => _error.ShouldBeNull();
    [Fact] void should_discover_the_event_type() => _result.ShouldContainOnly(_eventTypes.GetEventTypeFor(typeof(MyEvent)));

    public interface IService;

    public record Claimed;

    public class ClaimedReactor
    {
        public Claimed Handle(MyEvent @event, IService service) => new();
    }
}
