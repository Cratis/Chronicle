// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.Services.Projections.for_Projections.given;

public class a_mixed_all_event_preview : all_dependencies
{
    protected static readonly EventType Mapped = new("A", EventTypeGeneration.First);
    protected static readonly EventType Unmapped = new("B", EventTypeGeneration.First);
    protected AppendedEvent[] _previewedEvents;

    void Establish()
    {
        _projectionDefinition = _projectionDefinition with
        {
            SubscribesToAllEvents = true,
            From = new Dictionary<EventType, FromDefinition> { [Mapped] = new(new Dictionary<Properties.PropertyPath, string>(), new(string.Empty), null) }
        };
        SetCompiledDefinition(_projectionDefinition);
        _eventTypesStorage.GetLatestForAllEventTypes().Returns([new EventTypeSchema(Mapped, EventTypeOwner.Client, EventTypeSource.Code, new JsonSchema())]);
        _projectionGrain.GetEventTypes().Returns([Mapped]);
        _projectionGrain.GetEventTypesForPreview(Arg.Any<ReadModelDefinition>()).Returns([Mapped]);
        AppendedEvent[] stored =
        [
            new(EventContext.EmptyWithEventSourceId("source") with { EventType = Mapped, SequenceNumber = 1 }, new()),
            new(EventContext.EmptyWithEventSourceId("source") with { EventType = Unmapped, SequenceNumber = 2 }, new()),
            new(EventContext.EmptyWithEventSourceId("source") with { EventType = Mapped, SequenceNumber = 3 }, new())
        ];
        _eventSequenceStorage.GetEventsWithLimit(EventSequenceNumber.First, 1000, eventTypes: Arg.Any<IEnumerable<EventType>>())
            .Returns(call =>
            {
                var filter = call.Arg<IEnumerable<EventType>>().ToArray();
                var cursor = Substitute.For<IEventCursor>();
                cursor.MoveNext().Returns(true, false);
                cursor.Current.Returns(stored.Where(@event => filter.Length == 0 || filter.Contains(@event.Context.EventType)).ToArray());
                return cursor;
            });
        _projectionGrain.Process(Arg.Any<EventStoreNamespaceName>(), Arg.Any<IEnumerable<AppendedEvent>>())
            .Returns(call =>
            {
                _previewedEvents = call.Arg<IEnumerable<AppendedEvent>>().ToArray();
                return (ExpandoObject[])[];
            });
        _projectionGrain.ProcessForPreview(Arg.Any<EventStoreNamespaceName>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ReadModelDefinition>())
            .Returns(call =>
            {
                _previewedEvents = call.Arg<IEnumerable<AppendedEvent>>().ToArray();
                return (ExpandoObject[])[];
            });
    }
}
