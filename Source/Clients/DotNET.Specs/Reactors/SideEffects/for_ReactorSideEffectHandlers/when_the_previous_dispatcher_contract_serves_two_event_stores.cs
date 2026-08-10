// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Reactors.SideEffects.for_ReactorSideEffectHandlers;

/// <summary>
/// The previous dispatcher call has no event-store parameter of its own. A context produced by Chronicle carries
/// the current store so the compatibility adapter can reach the corrected per-store path instead of consulting a
/// registry captured when the process-lifetime handler was built.
/// </summary>
public class when_the_previous_dispatcher_contract_serves_two_event_stores : Specification
{
    readonly SomeEvent _event = new();

    ReactorSideEffectHandlers _handlers;
    IEventStore _firstEventStore;
    IEventStore _secondEventStore;
    bool _firstResult;
    bool _secondResult;

    void Establish()
    {
        _handlers = new ReactorSideEffectHandlers(new KnownInstancesOf<IReactorSideEffectHandler>([new EventResultHandler()]));
        _firstEventStore = EventStoreWhere(knowsTheEvent: true);
        _secondEventStore = EventStoreWhere(knowsTheEvent: false);
    }

    void Because()
    {
        _firstResult = _handlers.CanHandle(ContextFor(_firstEventStore), _event);
        _secondResult = _handlers.CanHandle(ContextFor(_secondEventStore), _event);
    }

    [Fact] void should_answer_from_the_first_event_stores_registry() => _firstResult.ShouldBeTrue();
    [Fact] void should_answer_from_the_second_event_stores_registry() => _secondResult.ShouldBeFalse();

    static ReactorContext ContextFor(IEventStore eventStore) =>
        new(EventContext.Empty, new object(), ReactorContextValues.Empty)
        {
            EventStore = eventStore
        };

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
