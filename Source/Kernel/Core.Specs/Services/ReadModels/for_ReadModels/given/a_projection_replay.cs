// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.ReadModels;

namespace Cratis.Chronicle.Services.ReadModels.for_ReadModels.given;

public class a_projection_replay : all_dependencies
{
    protected static readonly EventType Mapped = new("A", EventTypeGeneration.First);
    protected static readonly EventType Unmapped = new("B", EventTypeGeneration.First);
    protected IProjection _projection;
    protected ProjectionDefinition _definition;
    protected AppendedEvent[] _processedEvents;

    void Establish()
    {
        _definition = new ProjectionDefinition(
            ProjectionOwner.Client,
            EventSequenceId.Log,
            "mixed-all",
            _readModelDefinition.Identifier,
            true,
            true,
            new(),
            new Dictionary<EventType, FromDefinition> { [Mapped] = new(new Dictionary<PropertyPath, string> { ["Name"] = "name" }, PropertyExpression.NotSet, null) },
            new Dictionary<EventType, JoinDefinition>(),
            new Dictionary<PropertyPath, ChildrenDefinition>(),
            [],
            new FromEveryDefinition(new Dictionary<PropertyPath, string> { ["Marker"] = "$eventContext(sequenceNumber)" }, false),
            new Dictionary<EventType, RemovedWithDefinition>(),
            new Dictionary<EventType, RemovedWithJoinDefinition>());
        _projection = Substitute.For<IProjection>();
        _projection.GetDefinition().Returns(_ => _definition);
        _projection.GetEventTypes().Returns([Mapped]);
        _projection.Process(Arg.Any<Concepts.EventStoreNamespaceName>(), Arg.Any<IEnumerable<AppendedEvent>>())
            .Returns(call =>
            {
                _processedEvents = call.Arg<IEnumerable<AppendedEvent>>().ToArray();
                return (System.Dynamic.ExpandoObject[])[];
            });
        _grainFactory.GetGrain<IProjection>(Arg.Any<string>()).Returns(_projection);
        var definitions = Substitute.For<IReadModelDefinitionsStorage>();
        definitions.Get(_readModelDefinition.Identifier).Returns(_readModelDefinition);
        _eventStoreStorage.ReadModels.Returns(definitions);
        var sequence = Substitute.For<IEventSequenceStorage>();
        _namespaceStorage.GetEventSequence(EventSequenceId.Log).Returns(sequence);
        AppendedEvent[] stored =
        [
            new(EventContext.EmptyWithEventSourceId("source") with { EventType = Mapped, SequenceNumber = 1 }, new()),
            new(EventContext.EmptyWithEventSourceId("source") with { EventType = Unmapped, SequenceNumber = 2 }, new()),
            new(EventContext.EmptyWithEventSourceId("source") with { EventType = Mapped, SequenceNumber = 3 }, new())
        ];
        sequence.GetEventsWithLimit(EventSequenceNumber.First, 3, eventTypes: Arg.Any<IEnumerable<EventType>>())
            .Returns(call => CursorFor(stored, call.Arg<IEnumerable<EventType>>()));
        sequence.GetFromSequenceNumber(EventSequenceNumber.First, eventSourceId: null, eventTypes: Arg.Any<IEnumerable<EventType>>())
            .Returns(call => CursorFor(stored, call.Arg<IEnumerable<EventType>>()));
    }

    static IEventCursor CursorFor(IEnumerable<AppendedEvent> events, IEnumerable<EventType> eventTypes)
    {
        var filter = eventTypes.ToArray();
        var cursor = Substitute.For<IEventCursor>();
        cursor.MoveNext().Returns(true, false);
        cursor.Current.Returns(events.Where(@event => filter.Length == 0 || filter.Contains(@event.Context.EventType)).ToArray());
        return cursor;
    }
}
