// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
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
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Projections.Engine.given;

/// <summary>
/// Builds a real projection, through the <see cref="ProjectionFactory"/>, in which several event types reach the root
/// read model only through its children, each by a different registration.
/// </summary>
/// <remarks>
/// The root handles <see cref="RootCreated"/> with a from and <see cref="RootDerived"/> as a derivative that its
/// items children also handle. items handles <see cref="ItemAdded"/> with a from,
/// <see cref="ItemAttached"/> only as the value of an event property, and <see cref="DetailSet"/> on a nested object, and it
/// has a from-every clause. items has its own children subs that handle <see cref="SubAdded"/> with a
/// from and <see cref="SubAttached"/> only as the value of an event property.
/// </remarks>
public static class ProjectionWithChildOnlyEvents
{
    public static readonly EventType RootCreated = "RootCreated";
    public static readonly EventType RootDerived = "RootDerived";
    public static readonly EventType ItemAdded = "ItemAdded";
    public static readonly EventType ItemAttached = "ItemAttached";
    public static readonly EventType DetailSet = "DetailSet";
    public static readonly EventType SubAdded = "SubAdded";
    public static readonly EventType SubAttached = "SubAttached";
    public static readonly EventType Unrelated = "Unrelated";

    public static async Task<IProjection> Create()
    {
        EventStoreName eventStore = "event-store";
        EventStoreNamespaceName @namespace = "namespace";
        var storage = Substitute.For<IStorage>();
        var eventStoreStorage = Substitute.For<IEventStoreStorage>();
        var namespaceStorage = Substitute.For<IEventStoreNamespaceStorage>();
        var eventSequenceStorage = Substitute.For<IEventSequenceStorage>();
        storage.GetEventStore(eventStore).Returns(eventStoreStorage);
        eventStoreStorage.GetNamespace(@namespace).Returns(namespaceStorage);
        namespaceStorage.GetEventSequence(EventSequenceId.Log).Returns(eventSequenceStorage);

        var formats = new TypeFormats();
        var keyResolvers = new KeyResolvers(NullLogger<KeyResolvers>.Instance);
        var valueResolvers = new EventValueProviderExpressionResolvers(formats, NullLogger<EventValueProviderExpressionResolvers>.Instance);
        var factory = new ProjectionFactory(
            new ReadModelPropertyExpressionResolvers(valueResolvers, formats, NullLogger<ReadModelPropertyExpressionResolvers>.Instance),
            valueResolvers,
            new KeyExpressionResolvers(valueResolvers, keyResolvers, NullLogger<KeyExpressionResolvers>.Instance),
            new ExpandoObjectConverter(formats),
            keyResolvers,
            storage,
            NullLogger<ProjectionFactory>.Instance);

        var schema = await JsonSchema.FromJsonAsync("""
            {
              "type": "object",
              "properties": {
                "id": { "type": "string" },
                "name": { "type": "string" },
                "items": {
                  "type": "array",
                  "items": {
                    "type": "object",
                    "properties": {
                      "id": { "type": "string" },
                      "label": { "type": "string" },
                      "count": { "type": "integer" },
                      "detail": { "type": "object", "properties": { "note": { "type": "string" } } },
                      "subs": {
                        "type": "array",
                        "items": { "type": "object", "properties": { "id": { "type": "string" }, "value": { "type": "string" } } }
                      }
                    }
                  }
                }
              }
            }
            """);
        var readModel = new ReadModelDefinition(
            "test-model",
            "test-model",
            "test-model",
            ReadModelOwner.Client,
            ReadModelSource.Code,
            ReadModelObserverType.Projection,
            ReadModelObserverIdentifier.Unspecified,
            SinkDefinition.None,
            new Dictionary<ReadModelGeneration, JsonSchema> { [ReadModelGeneration.First] = schema },
            []);

        return await factory.Create(eventStore, @namespace, CreateDefinition(), readModel, []);
    }

    static FromDefinition EmptyFrom() => new(new Dictionary<PropertyPath, string>(), PropertyExpression.NotSet, null);

    static ProjectionDefinition CreateDefinition()
    {
        var subs = new ChildrenDefinition(
            (PropertyPath)"id",
            new Dictionary<EventType, FromDefinition> { [SubAdded] = EmptyFrom() },
            new Dictionary<EventType, JoinDefinition>(),
            new Dictionary<PropertyPath, ChildrenDefinition>(),
            new FromEveryDefinition(new Dictionary<PropertyPath, string>(), false),
            new Dictionary<EventType, RemovedWithDefinition>(),
            new Dictionary<EventType, RemovedWithJoinDefinition>(),
            new FromEventPropertyDefinition(SubAttached, "sub"),
            AutoMap.Disabled);

        var items = new ChildrenDefinition(
            (PropertyPath)"id",
            new Dictionary<EventType, FromDefinition>
            {
                [ItemAdded] = EmptyFrom(),
                [RootDerived] = EmptyFrom()
            },
            new Dictionary<EventType, JoinDefinition>(),
            new Dictionary<PropertyPath, ChildrenDefinition> { [(PropertyPath)"subs"] = subs },
            new FromEveryDefinition(new Dictionary<PropertyPath, string> { [(PropertyPath)"count"] = WellKnownExpressions.Count }, false),
            new Dictionary<EventType, RemovedWithDefinition>(),
            new Dictionary<EventType, RemovedWithJoinDefinition>(),
            new FromEventPropertyDefinition(ItemAttached, "item"),
            AutoMap.Disabled,
            new Dictionary<PropertyPath, ChildrenDefinition>
            {
                [(PropertyPath)"detail"] = new ChildrenDefinition(
                    PropertyPath.Root,
                    new Dictionary<EventType, FromDefinition> { [DetailSet] = EmptyFrom() },
                    new Dictionary<EventType, JoinDefinition>(),
                    new Dictionary<PropertyPath, ChildrenDefinition>(),
                    new FromEveryDefinition(new Dictionary<PropertyPath, string>(), false),
                    new Dictionary<EventType, RemovedWithDefinition>(),
                    new Dictionary<EventType, RemovedWithJoinDefinition>(),
                    AutoMap: AutoMap.Disabled)
            });

        return new ProjectionDefinition(
            ProjectionOwner.Client,
            EventSequenceId.Log,
            "test-projection",
            "test-model",
            true,
            true,
            new(),
            new Dictionary<EventType, FromDefinition> { [RootCreated] = EmptyFrom() },
            new Dictionary<EventType, JoinDefinition>(),
            new Dictionary<PropertyPath, ChildrenDefinition> { [(PropertyPath)"items"] = items },
            [new FromDerivatives([RootDerived], EmptyFrom())],
            new FromEveryDefinition(new Dictionary<PropertyPath, string>(), false),
            new Dictionary<EventType, RemovedWithDefinition>(),
            new Dictionary<EventType, RemovedWithJoinDefinition>(),
            AutoMap: AutoMap.Disabled);
    }
}
