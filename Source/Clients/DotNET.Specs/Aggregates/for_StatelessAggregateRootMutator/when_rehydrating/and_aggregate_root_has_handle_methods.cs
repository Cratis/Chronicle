// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Dynamic;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Aggregates.for_StatelessAggregateRootMutator.when_rehydrating;

public class and_aggregate_root_has_handle_methods : given.a_stateless_aggregate_root_mutator
{
    ExpandoObject _firstEvent;
    ExpandoObject _secondEvent;
    IEnumerable<ExpandoObject> _events;
    IImmutableList<AppendedEvent> _appendedEvents;

    void Establish()
    {
        _firstEvent = new ExpandoObject();
        ((dynamic)_firstEvent).Something = "FirstEvent";
        _secondEvent = new ExpandoObject();
        ((dynamic)_secondEvent).Something = "SecondEvent";

        _events =
        [
            _firstEvent,
            _secondEvent
        ];

        _appendedEvents = new AppendedEvent[]
        {
            AppendedEvent.EmptyWithContent(_firstEvent),
            AppendedEvent.EmptyWithContent(_secondEvent)
        }.ToImmutableList();

        _eventHandlers.HasHandleMethods.Returns(true);
        _eventSequence
            .GetForEventSourceIdAndEventTypes(_eventSourceId, Arg.Any<IEnumerable<EventType>>())
            .Returns(_appendedEvents);

        _eventSerializer
            .Deserialize(Arg.Any<AppendedEvent>())
            .Returns((callInfo) => callInfo.Arg<AppendedEvent>().Content);
    }

    async Task Because() => await _mutator.Rehydrate();

    [Fact] void should_handle_events() => _eventHandlers.Received().Handle(_aggregateRoot, Arg.Is<IEnumerable<EventAndContext>>(arg => arg.Select(_ => _.Event).SequenceEqual(_events)));
    [Fact] void should_not_ask_if_there_are_events_for_the_event_source_id() => _eventSequence.DidNotReceive().HasEventsFor(_eventSourceId);
    [Fact] void should_set_has_events_for_rehydration_to_true() => _aggregateRootContext.HasEventsForRehydration.ShouldBeTrue();
}
