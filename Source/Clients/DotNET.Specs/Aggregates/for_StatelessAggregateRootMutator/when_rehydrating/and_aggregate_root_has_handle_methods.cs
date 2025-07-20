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

        _appendedEvents = new[]
        {
            AppendedEvent.EmptyWithContent(_firstEvent),
            AppendedEvent.EmptyWithContent(_secondEvent)
        }.ToImmutableList();

        _eventHandlers.HasHandleMethods.Returns(true);
        _eventSequence
            .GetFromSequenceNumber(EventSequenceNumber.First, _eventSourceId, Arg.Any<IEnumerable<EventType>>())
            .Returns(_appendedEvents);

        _eventSequence
            .GetTailSequenceNumber(_eventSourceId)
            .Returns(42UL);

        _eventSerializer
            .Deserialize(Arg.Any<AppendedEvent>())
            .Returns((callInfo) => callInfo.Arg<AppendedEvent>().Content);
    }

    async Task Because() => await _mutator.Rehydrate();

    [Fact] void should_handle_events() => _eventHandlers.Received().Handle(_aggregateRoot, Arg.Is<IEnumerable<EventAndContext>>(arg => arg.Select(_ => _.Event).SequenceEqual(_events)), Arg.Any<Action<EventAndContext>>());
    [Fact] void should_ask_if_there_are_events_for_the_event_source_id() => _eventSequence.Received(1).GetFromSequenceNumber(EventSequenceNumber.First, _eventSourceId, _eventHandlers.EventTypes);
}
