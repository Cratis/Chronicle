// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Projections.Engine.Expressions;
using Cratis.Chronicle.Projections.Engine.Expressions.EventValues;
using Cratis.Chronicle.Projections.Engine.Expressions.Keys;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.EventTypes;
using Cratis.Monads;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionFactory.when_creating.given;

/// <summary>
/// One definition and one factory serving two namespaces whose event sequences hold different joined data.
/// </summary>
public class a_factory_for_two_namespaces : Specification
{
    protected static readonly EventStoreName _eventStore = "event-store";
    protected static readonly EventStoreNamespaceName _first = "first";
    protected static readonly EventStoreNamespaceName _second = "second";
    protected static readonly EventType _created = new("Created", EventTypeGeneration.First);
    protected static readonly EventType _joined = new("Joined", EventTypeGeneration.First);
    protected ProjectionFactory _factory;
    protected ProjectionDefinition _definition;
    protected ReadModelDefinition _readModel;
    protected IEventSequenceStorage _firstStorage;
    protected IEventSequenceStorage _secondStorage;
    protected IEventTypesStorage _eventTypes;
    protected IStorage _storage;

    /// <summary>
    /// Projects a created event through a projection.
    /// </summary>
    /// <param name="projection">The <see cref="IProjection"/> to push the event through.</param>
    /// <returns>The changeset the event was projected into.</returns>
    protected static Changeset<AppendedEvent, ExpandoObject> Project(IProjection projection)
    {
        dynamic content = new ExpandoObject();
        content.joinId = "shared-join-id";
        var created = new AppendedEvent(
            EventContext.EmptyWithEventSourceId("root") with { EventType = _created, SequenceNumber = 5 },
            (ExpandoObject)content);
        var changeset = new Changeset<AppendedEvent, ExpandoObject>(new ObjectComparer(), created, new ExpandoObject());
        projection.OnNext(new ProjectionEventContext(new Key("root", ArrayIndexers.NoIndexers), created, changeset, ProjectionOperationType.From, false, "root"));
        return changeset;
    }

    protected static object? NameOf(Changeset<AppendedEvent, ExpandoObject> changeset)
    {
        var joined = changeset.Changes.OfType<ResolvedJoin>().LastOrDefault();
        var state = joined?.Changes.OfType<PropertiesChanged<ExpandoObject>>().LastOrDefault()?.State ?? changeset.CurrentState;
        return ((IDictionary<string, object?>)state).TryGetValue("name", out var name) ? name : null;
    }

    static IEventSequenceStorage StorageFor(IStorage storage, EventStoreNamespaceName @namespace, string joinedName)
    {
        dynamic content = new ExpandoObject();
        content.name = joinedName;
        var stored = new AppendedEvent(
            EventContext.EmptyWithEventSourceId("shared-join-id") with { EventType = _joined, SequenceNumber = 1 },
            (ExpandoObject)content);
        var eventSequenceStorage = Substitute.For<IEventSequenceStorage>();
        eventSequenceStorage
            .TryGetLastEventBefore(Arg.Any<EventTypeId>(), Arg.Any<EventSourceId>(), Arg.Any<EventSequenceNumber>())
            .Returns(Task.FromResult(Catch<Option<AppendedEvent>>.Success(new Option<AppendedEvent>(stored))));
        storage.GetEventStore(_eventStore).GetNamespace(@namespace).GetEventSequence(EventSequenceId.Log).Returns(eventSequenceStorage);
        return eventSequenceStorage;
    }

    async Task Establish()
    {
        var storage = _storage = Substitute.For<IStorage>();
        _firstStorage = StorageFor(storage, _first, "alpha");
        _secondStorage = StorageFor(storage, _second, "beta");
        _eventTypes = storage.GetEventStore(_eventStore).EventTypes;
        _eventTypes.GetLatestForAllEventTypes().Returns(Task.FromResult<IEnumerable<EventTypeSchema>>([]));

        var formats = new TypeFormats();
        var keys = new KeyResolvers(NullLogger<KeyResolvers>.Instance);
        var values = new EventValueProviderExpressionResolvers(formats, NullLogger<EventValueProviderExpressionResolvers>.Instance);
        _factory = new(
            new ReadModelPropertyExpressionResolvers(values, formats, NullLogger<ReadModelPropertyExpressionResolvers>.Instance),
            values,
            new KeyExpressionResolvers(values, keys, NullLogger<KeyExpressionResolvers>.Instance),
            new ExpandoObjectConverter(formats),
            keys,
            storage,
            NullLogger<ProjectionFactory>.Instance);

        _definition = new(
            ProjectionOwner.Client,
            EventSequenceId.Log,
            "joined-projection",
            "joined-model",
            true,
            true,
            new(),
            new Dictionary<EventType, FromDefinition>
            {
                [_created] = new(new Dictionary<PropertyPath, string> { ["joinId"] = "joinId" }, PropertyExpression.NotSet, null)
            },
            new Dictionary<EventType, JoinDefinition>
            {
                [_joined] = new("joinId", new Dictionary<PropertyPath, string> { ["name"] = "name" }, PropertyExpression.NotSet)
            },
            new Dictionary<PropertyPath, ChildrenDefinition>(),
            [],
            new FromEveryDefinition(new Dictionary<PropertyPath, string>(), false),
            new Dictionary<EventType, RemovedWithDefinition>(),
            new Dictionary<EventType, RemovedWithJoinDefinition>(),
            AutoMap: AutoMap.Disabled);

        var schema = await JsonSchema.FromJsonAsync("""
            {"type":"object","properties":{"id":{"type":"string"},"joinId":{"type":"string"},"name":{"type":"string"}}}
            """);
        _readModel = new(
            "joined-model",
            "joined-model",
            "joined-model",
            ReadModelOwner.Client,
            ReadModelSource.Code,
            ReadModelObserverType.Projection,
            ReadModelObserverIdentifier.Unspecified,
            SinkDefinition.None,
            new Dictionary<ReadModelGeneration, JsonSchema> { [ReadModelGeneration.First] = schema },
            []);
    }
}
