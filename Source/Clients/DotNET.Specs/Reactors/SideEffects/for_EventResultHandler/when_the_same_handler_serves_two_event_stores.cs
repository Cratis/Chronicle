// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Reactors.SideEffects.for_EventResultHandler;

/// <summary>
/// The handler is a process-lifetime singleton, but the event type registry it judges against belongs to the
/// event store — and therefore to the namespace — the current scope resolved. One instance must answer for each
/// store it is handed, not for whichever one happened to build it first.
/// </summary>
public class when_the_same_handler_serves_two_event_stores : Specification
{
    readonly EventResultHandler _handler = new();
    readonly SomeEvent _event = new();

    IEventStore _knowsTheEvent;
    IEventStore _doesNotKnowTheEvent;
    ReactorContext _reactorContext;

    bool _resultForTheStoreThatKnowsIt;
    bool _resultForTheStoreThatDoesNot;

    void Establish()
    {
        _knowsTheEvent = EventStoreWhere(knowsTheEvent: true);
        _doesNotKnowTheEvent = EventStoreWhere(knowsTheEvent: false);
        _reactorContext = new ReactorContext(EventContext.Empty, new object(), ReactorContextValues.Empty);
    }

    void Because()
    {
        _resultForTheStoreThatKnowsIt = _handler.CanHandle(_reactorContext, _knowsTheEvent, _event);
        _resultForTheStoreThatDoesNot = _handler.CanHandle(_reactorContext, _doesNotKnowTheEvent, _event);
    }

    [Fact] void should_handle_it_for_the_store_that_knows_the_event_type() => _resultForTheStoreThatKnowsIt.ShouldBeTrue();
    [Fact] void should_not_handle_it_for_the_store_that_does_not() => _resultForTheStoreThatDoesNot.ShouldBeFalse();

    static IEventStore EventStoreWhere(bool knowsTheEvent)
    {
        var eventTypes = Substitute.For<IEventTypes>();
        eventTypes.HasFor(typeof(SomeEvent)).Returns(knowsTheEvent);

        var eventStore = Substitute.For<IEventStore>();
        eventStore.EventTypes.Returns(eventTypes);
        return eventStore;
    }

    record SomeEvent;
}
