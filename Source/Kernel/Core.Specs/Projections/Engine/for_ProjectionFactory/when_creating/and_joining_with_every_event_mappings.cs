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

public class and_joining_with_every_event_mappings : Specification
{
    int _withChildren;
    int _withoutChildren;

    async Task Because()
    {
        _withChildren = await SubscriptionCount(true);
        _withoutChildren = await SubscriptionCount(false);
    }

    [Fact] void should_not_add_an_extra_join_projection_when_children_are_included() => _withChildren.ShouldEqual(_withoutChildren);
    [Fact] void should_subscribe_once_for_each_from_join_and_join_backfill() => _withChildren.ShouldEqual(3);

    static async Task<int> SubscriptionCount(bool includeChildren)
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

        var definition = new ProjectionDefinition(
            ProjectionOwner.Client,
            EventSequenceId.Log,
            "test-projection",
            "test-model",
            true,
            true,
            new(),
            new Dictionary<EventType, FromDefinition>
            {
                [(EventType)"Created"] = new FromDefinition(new Dictionary<PropertyPath, string> { [(PropertyPath)"joinId"] = "joinId" }, PropertyExpression.NotSet, null)
            },
            new Dictionary<EventType, JoinDefinition>
            {
                [(EventType)"Joined"] = new JoinDefinition((PropertyPath)"joinId", new Dictionary<PropertyPath, string>(), PropertyExpression.NotSet)
            },
            new Dictionary<PropertyPath, ChildrenDefinition>(),
            [],
            new FromEveryDefinition(new Dictionary<PropertyPath, string> { [(PropertyPath)"count"] = WellKnownExpressions.Count }, includeChildren),
            new Dictionary<EventType, RemovedWithDefinition>(),
            new Dictionary<EventType, RemovedWithJoinDefinition>(),
            AutoMap: AutoMap.Disabled);
        var schema = await JsonSchema.FromJsonAsync("""
            {"type":"object","properties":{"id":{"type":"string"},"joinId":{"type":"string"},"count":{"type":"integer"}}}
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
        var projection = await factory.Create(eventStore, @namespace, definition, readModel, []);

        return ((Projection)projection).Subscriptions.Count;
    }
}
