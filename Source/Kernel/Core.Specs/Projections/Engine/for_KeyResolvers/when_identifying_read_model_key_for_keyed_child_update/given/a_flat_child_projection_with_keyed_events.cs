// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Dynamic;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.Sinks;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Projections.Engine.for_KeyResolvers.when_identifying_read_model_key_for_keyed_child_update.given;

/// <summary>
/// Sets up a flat two-level projection hierarchy: Root → Child, where all child events are appended
/// to the root's event source and carry the child's key in their content. The child is created by
/// one event type and updated by another — the shape that produced per-event re-resolution of the
/// child's creation event for every subsequent event on the same event source.
/// </summary>
public class a_flat_child_projection_with_keyed_events : Specification
{
    protected const string BoardKey = "board-key";
    protected const string ChildKey = "child-key";

    protected static readonly EventType RootCreatedEventType = new("root-created-event-type", 1);
    protected static readonly EventType ChildAddedEventType = new("child-added-event-type", 1);
    protected static readonly EventType ChildMovedEventType = new("child-moved-event-type", 1);

    protected AppendedEvent RootCreatedEvent;
    protected AppendedEvent ChildAddedEvent;
    protected AppendedEvent ChildMovedEvent;

    protected IProjection RootProjection;
    protected IProjection ChildProjection;

    protected IEventSequenceStorage Storage;
    protected ISink Sink;
    protected KeyResolvers KeyResolvers;

    void Establish()
    {
        Storage = Substitute.For<IEventSequenceStorage>();
        Sink = Substitute.For<ISink>();
        KeyResolvers = new KeyResolvers(NullLogger<KeyResolvers>.Instance);

        RootCreatedEvent = CreateEvent(0, RootCreatedEventType, BoardKey, new ExpandoObject());
        ChildAddedEvent = CreateEvent(5, ChildAddedEventType, BoardKey, new { childId = ChildKey }.AsExpandoObject());
        ChildMovedEvent = CreateEvent(7, ChildMovedEventType, BoardKey, new { childId = ChildKey }.AsExpandoObject());

        RootProjection = Substitute.For<IProjection>();
        RootProjection.HasParent.Returns(false);
        RootProjection.Parent.Returns((IProjection)null);
        RootProjection.ChildrenPropertyPath.Returns(PropertyPath.NotSet);
        RootProjection.IdentifiedByProperty.Returns((PropertyPath)"id");
        RootProjection.Path.Returns((ProjectionPath)"root");
        RootProjection.OwnEventTypes.Returns([RootCreatedEventType]);
        RootProjection.EventTypes.Returns([RootCreatedEventType]);
        RootProjection.GetKeyResolverFor(RootCreatedEventType).Returns((_, _, _) =>
            Task.FromResult(KeyResolverResult.Resolved(new Key(BoardKey, ArrayIndexers.NoIndexers))));

        ChildProjection = Substitute.For<IProjection>();
        ChildProjection.HasParent.Returns(true);
        ChildProjection.Parent.Returns(RootProjection);
        ChildProjection.ChildrenPropertyPath.Returns((PropertyPath)"children");
        ChildProjection.IdentifiedByProperty.Returns((PropertyPath)"childId");
        ChildProjection.Path.Returns((ProjectionPath)"children");
        ChildProjection.OwnEventTypes.Returns([ChildAddedEventType, ChildMovedEventType]);
        ChildProjection.EventTypes.Returns([ChildAddedEventType, ChildMovedEventType]);

        // Wire the real resolvers so any re-resolution of the child's creation event runs the
        // full chain, exactly as it does in production.
        var childAddedKeyResolver = CreateResolverUnderTest();
        ChildProjection.GetKeyResolverFor(ChildAddedEventType).Returns(childAddedKeyResolver);
        var childMovedKeyResolver = CreateResolverUnderTest();
        ChildProjection.GetKeyResolverFor(ChildMovedEventType).Returns(childMovedKeyResolver);
    }

    protected KeyResolver CreateResolverUnderTest() =>
        KeyResolvers.FromParentHierarchy(
            ChildProjection,
            KeyResolvers.FromEventValueProvider(EventValueProviders.EventContent("childId")),
            KeyResolvers.FromEventSourceId,
            "childId");

    protected static AppendedEvent CreateEvent(ulong sequenceNumber, EventType eventType, EventSourceId eventSourceId, ExpandoObject content) =>
        new(
            new(
                eventType,
                EventSourceType.Default,
                eventSourceId,
                EventStreamType.All,
                EventStreamId.Default,
                sequenceNumber,
                DateTimeOffset.UtcNow,
                "123b8935-a1a4-410d-aace-e340d48f0aa0",
                "41f18595-4748-4b01-88f7-4c0d0907aa90",
                CorrelationId.New(),
                [],
                Identity.System,
                [],
                EventHash.NotSet),
            content);

    protected IEventCursor CreateCursorWith(params AppendedEvent[] events)
    {
        var cursor = Substitute.For<IEventCursor>();
        cursor.Current.Returns(events);
        var callCount = 0;
        cursor.MoveNext().Returns(_ => Task.FromResult(callCount++ == 0));
        return cursor;
    }
}
