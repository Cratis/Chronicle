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

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionFactory.when_creating.given;

public class a_child_collection_projection : Specification
{
    protected static readonly EventStoreName _eventStore = "event-store";
    protected static readonly EventStoreNamespaceName _namespace = "namespace";
    protected ProjectionFactory _factory;
    protected ProjectionDefinition _definition;
    protected ReadModelDefinition _readModel;

    void Establish()
    {
        var storage = Substitute.For<IStorage>();
        var eventStoreStorage = Substitute.For<IEventStoreStorage>();
        var namespaceStorage = Substitute.For<IEventStoreNamespaceStorage>();
        storage.GetEventStore(_eventStore).Returns(eventStoreStorage);
        eventStoreStorage.GetNamespace(_namespace).Returns(namespaceStorage);
        namespaceStorage.GetEventSequence(EventSequenceId.Log).Returns(Substitute.For<IEventSequenceStorage>());

        var typeFormats = new TypeFormats();
        var keyResolvers = new KeyResolvers(NullLogger<KeyResolvers>.Instance);
        var eventResolvers = new EventValueProviderExpressionResolvers(typeFormats, NullLogger<EventValueProviderExpressionResolvers>.Instance);
        _factory = new(
            new ReadModelPropertyExpressionResolvers(eventResolvers, typeFormats, NullLogger<ReadModelPropertyExpressionResolvers>.Instance),
            eventResolvers,
            new KeyExpressionResolvers(eventResolvers, keyResolvers, NullLogger<KeyExpressionResolvers>.Instance),
            new ExpandoObjectConverter(typeFormats),
            keyResolvers,
            storage,
            NullLogger<ProjectionFactory>.Instance);

        _definition = new(
            ProjectionOwner.Client,
            EventSequenceId.Log,
            "Alerts",
            "Alert",
            true,
            true,
            new(),
            new Dictionary<EventType, FromDefinition>(),
            new Dictionary<EventType, JoinDefinition>(),
            new Dictionary<PropertyPath, ChildrenDefinition> { ["notes"] = Child() },
            [],
            new FromEveryDefinition(new Dictionary<PropertyPath, string>(), false),
            new Dictionary<EventType, RemovedWithDefinition>(),
            new Dictionary<EventType, RemovedWithJoinDefinition>(),
            AutoMap: AutoMap.Disabled);

        _readModel = new(
            "Alert",
            "alerts",
            "Alert",
            ReadModelOwner.Client,
            ReadModelSource.Code,
            ReadModelObserverType.Projection,
            ReadModelObserverIdentifier.Unspecified,
            SinkDefinition.None,
            new Dictionary<ReadModelGeneration, JsonSchema>
            {
                [ReadModelGeneration.First] = JsonSchema.FromJson("""
                    {
                      "type": "object",
                      "properties": {
                        "Id": { "type": "string" },
                        "Notes": {
                          "type": "array",
                          "items": {
                            "type": "object",
                            "properties": {
                              "Id": { "type": "integer" },
                              "Replies": {
                                "type": "array",
                                "items": {
                                  "type": "object",
                                  "properties": { "Id": { "type": "string" } }
                                }
                              }
                            }
                          }
                        }
                      }
                    }
                    """)
            },
            []);
    }

    protected static ChildrenDefinition Child() => new(
        "id",
        new Dictionary<EventType, FromDefinition>(),
        new Dictionary<EventType, JoinDefinition>(),
        new Dictionary<PropertyPath, ChildrenDefinition>(),
        new FromEveryDefinition(new Dictionary<PropertyPath, string>(), false),
        new Dictionary<EventType, RemovedWithDefinition>(),
        new Dictionary<EventType, RemovedWithJoinDefinition>(),
        AutoMap: AutoMap.Disabled);
}
