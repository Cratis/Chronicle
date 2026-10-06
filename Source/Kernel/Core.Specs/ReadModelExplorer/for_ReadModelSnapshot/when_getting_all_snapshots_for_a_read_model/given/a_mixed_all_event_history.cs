// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.ReadModelExplorer.for_ReadModelSnapshot.when_getting_all_snapshots_for_a_read_model.given;

public class a_mixed_all_event_history : a_projection_with_a_history
{
    protected ProjectionDefinition _definition;
    protected AppendedEvent[] _events;

    async Task Establish()
    {
        var mapped = new EventType("A", EventTypeGeneration.First);
        var unmapped = new EventType("B", EventTypeGeneration.First);
        _definition = await _projection.GetDefinition();
        _definition = _definition with
        {
            SubscribesToAllEvents = true,
            From = new Dictionary<EventType, FromDefinition> { [mapped] = new(new Dictionary<PropertyPath, string>(), new(string.Empty), null) }
        };
        _projection.GetDefinition().Returns(_ => _definition);
        _projection.GetEventTypes().Returns([mapped]);
        _events =
        [
            new(EventContext.EmptyWithEventSourceId("my-instance") with { EventType = mapped, SequenceNumber = 1, CorrelationId = FirstCorrelation }, new()),
            new(EventContext.EmptyWithEventSourceId("my-instance") with { EventType = unmapped, SequenceNumber = 2, CorrelationId = SecondCorrelation }, new()),
            new(EventContext.EmptyWithEventSourceId("my-instance") with { EventType = mapped, SequenceNumber = 3, CorrelationId = FirstCorrelation }, new())
        ];
        var sequence = _storage.GetEventStore("test-store").GetNamespace("test-namespace").GetEventSequence("event-log");
        sequence.GetFromSequenceNumber(EventSequenceNumber.First, Arg.Any<EventSourceId?>(), eventTypes: Arg.Any<IEnumerable<EventType>>())
            .Returns(call =>
            {
                var filter = call.Arg<IEnumerable<EventType>>().ToArray();
                var source = call.ArgAt<EventSourceId?>(1);
                var cursor = Substitute.For<IEventCursor>();
                cursor.MoveNext().Returns(true, false);
                cursor.Current.Returns(_events.Where(@event =>
                    (source?.IsSpecified != true || @event.Context.EventSourceId == source) &&
                    (filter.Length == 0 || filter.Contains(@event.Context.EventType))).ToArray());
                return cursor;
            });
        _projection.GetEventsForKey(Arg.Any<Concepts.EventStoreNamespaceName>(), "my-instance", Arg.Any<IEnumerable<AppendedEvent>>())
            .Returns(call => call.Arg<IEnumerable<AppendedEvent>>());
    }
}
