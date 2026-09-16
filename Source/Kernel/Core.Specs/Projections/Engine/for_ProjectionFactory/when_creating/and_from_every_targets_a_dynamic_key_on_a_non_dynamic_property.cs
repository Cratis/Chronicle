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

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionFactory.when_creating;

public class and_from_every_targets_a_dynamic_key_on_a_non_dynamic_property : Specification
{
    static readonly EventStoreName _eventStore = "event-store";
    static readonly EventStoreNamespaceName _namespace = "namespace";

    ProjectionFactory _factory;
    Exception _result;

    void Establish()
    {
        var storage = Substitute.For<IStorage>();
        var eventStoreStorage = Substitute.For<IEventStoreStorage>();
        var namespaceStorage = Substitute.For<IEventStoreNamespaceStorage>();
        var eventSequenceStorage = Substitute.For<IEventSequenceStorage>();
        storage.GetEventStore(_eventStore).Returns(eventStoreStorage);
        eventStoreStorage.GetNamespace(_namespace).Returns(namespaceStorage);
        namespaceStorage.GetEventSequence(EventSequenceId.Log).Returns(eventSequenceStorage);

        var typeFormats = new TypeFormats();
        var keyResolvers = new KeyResolvers(NullLogger<KeyResolvers>.Instance);
        var eventValueProviderExpressionResolvers = new EventValueProviderExpressionResolvers(
            typeFormats,
            NullLogger<EventValueProviderExpressionResolvers>.Instance);

        _factory = new ProjectionFactory(
            new ReadModelPropertyExpressionResolvers(
                eventValueProviderExpressionResolvers,
                typeFormats,
                NullLogger<ReadModelPropertyExpressionResolvers>.Instance),
            eventValueProviderExpressionResolvers,
            new KeyExpressionResolvers(
                eventValueProviderExpressionResolvers,
                keyResolvers,
                NullLogger<KeyExpressionResolvers>.Instance),
            new ExpandoObjectConverter(typeFormats),
            keyResolvers,
            storage,
            NullLogger<ProjectionFactory>.Instance);
    }

    async Task Because() => _result = await Catch.Exception(() => _factory.Create(
        _eventStore,
        _namespace,
        CreateProjectionDefinition(),
        CreateReadModelDefinition(),
        []));

    [Fact] void should_throw_dynamic_property_path_on_non_dynamic_property() =>
        _result.ShouldBeOfExactType<DynamicPropertyPathOnNonDynamicProperty>();

    static ProjectionDefinition CreateProjectionDefinition() => new(
        ProjectionOwner.Client,
        EventSequenceId.Log,
        "Core.Admin.Events.EventCounts",
        "Core.Admin.Events.EventCounts",
        true,
        true,
        new(),
        new Dictionary<EventType, FromDefinition>(),
        new Dictionary<EventType, JoinDefinition>(),
        new Dictionary<PropertyPath, ChildrenDefinition>(),
        [],
        new FromEveryDefinition(
            new Dictionary<PropertyPath, string>
            {
                [new PropertyPath("count.$eventContext.eventType.id")] = WellKnownExpressions.Count
            },
            true),
        new Dictionary<EventType, RemovedWithDefinition>(),
        new Dictionary<EventType, RemovedWithJoinDefinition>(),
        AutoMap: AutoMap.Disabled);

    static ReadModelDefinition CreateReadModelDefinition() =>
        new(
            "Core.Admin.Events.EventCounts",
            "eventCounts",
            "EventCounts",
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
                        "id": { "type": "string" },
                        "count": { "type": "integer" }
                      }
                    }
                    """)
            },
            []);
}
