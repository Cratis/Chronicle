// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Identities;
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
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionFactory.when_creating;

public class and_joining_with_every_event_mappings : Specification
{
    (int Subscriptions, long Count) _withChildren;
    (int Subscriptions, long Count) _withoutChildren;

    async Task Because()
    {
        _withChildren = await ProjectJoin(true);
        _withoutChildren = await ProjectJoin(false);
    }

    [Fact] void should_not_add_an_extra_join_projection_when_children_are_included() => _withChildren.Subscriptions.ShouldEqual(_withoutChildren.Subscriptions);
    [Fact] void should_subscribe_once_for_each_from_join_and_join_backfill() => _withChildren.Subscriptions.ShouldEqual(3);
    [Fact] void should_count_the_join_once_when_children_are_included() => _withChildren.Count.ShouldEqual(1);
    [Fact] void should_count_the_join_once_when_children_are_excluded() => _withoutChildren.Count.ShouldEqual(1);

    internal static async Task<(int Subscriptions, long Count)> ProjectJoin(bool includeChildren, bool isChild = false)
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

        var from = new Dictionary<EventType, FromDefinition>
        {
            [(EventType)"Created"] = new FromDefinition(new Dictionary<PropertyPath, string> { [(PropertyPath)"joinId"] = "joinId" }, PropertyExpression.NotSet, null)
        };
        var joins = new Dictionary<EventType, JoinDefinition>
        {
            [(EventType)"Joined"] = new JoinDefinition((PropertyPath)"joinId", new Dictionary<PropertyPath, string>(), PropertyExpression.NotSet)
        };
        var every = new FromEveryDefinition(new Dictionary<PropertyPath, string> { [(PropertyPath)"count"] = WellKnownExpressions.Count }, includeChildren);
        var children = isChild ? new Dictionary<PropertyPath, ChildrenDefinition>
        {
            [(PropertyPath)"items"] = new ChildrenDefinition(
                (PropertyPath)"id",
                from,
                joins,
                new Dictionary<PropertyPath, ChildrenDefinition>(),
                every,
                new Dictionary<EventType, RemovedWithDefinition>(),
                new Dictionary<EventType, RemovedWithJoinDefinition>(),
                AutoMap: AutoMap.Disabled)
        } : new Dictionary<PropertyPath, ChildrenDefinition>();
        var definition = new ProjectionDefinition(
            ProjectionOwner.Client,
            EventSequenceId.Log,
            "test-projection",
            "test-model",
            true,
            true,
            new(),
            isChild ? new Dictionary<EventType, FromDefinition>() : from,
            isChild ? new Dictionary<EventType, JoinDefinition>() : joins,
            children,
            [],
            isChild ? new FromEveryDefinition(new Dictionary<PropertyPath, string>(), false) : every,
            new Dictionary<EventType, RemovedWithDefinition>(),
            new Dictionary<EventType, RemovedWithJoinDefinition>(),
            AutoMap: AutoMap.Disabled);
        var schema = await JsonSchema.FromJsonAsync("""
            {"type":"object","properties":{"id":{"type":"string"},"joinId":{"type":"string"},"count":{"type":"integer"},"items":{"type":"array","items":{"type":"object","properties":{"id":{"type":"string"},"joinId":{"type":"string"},"count":{"type":"integer"}}}}}}
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
        var rootProjection = await factory.Create(eventStore, @namespace, definition, readModel, []);
        var projection = isChild ? rootProjection.ChildProjections.Single() : rootProjection;
        var incoming = new AppendedEvent(
            new(
                (EventType)"Joined",
                EventSourceType.Default,
                "join-source",
                EventStreamType.All,
                EventStreamId.Default,
                0,
                DateTimeOffset.UtcNow,
                "123b8935-a1a4-410d-aace-e340d48f0aa0",
                "41f18595-4748-4b01-88f7-4c0d0907aa90",
                CorrelationId.New(),
                [],
                Identity.System,
                [],
                EventHash.NotSet),
            new ExpandoObject());
        var state = new ExpandoObject();
        ((IDictionary<string, object?>)state)["count"] = 0L;
        if (isChild)
        {
            var item = new ExpandoObject();
            ((IDictionary<string, object?>)item)["id"] = "child";
            ((IDictionary<string, object?>)item)["joinId"] = "join-source";
            ((IDictionary<string, object?>)item)["count"] = 0L;
            ((IDictionary<string, object?>)state)["items"] = new List<ExpandoObject> { item };
        }
        var changeset = new Changeset<AppendedEvent, ExpandoObject>(new ObjectComparer(), incoming, state);
        var indexers = isChild
            ? new ArrayIndexers([new ArrayIndexer((PropertyPath)"[items]", (PropertyPath)"id", "child")])
            : ArrayIndexers.NoIndexers;
        projection.OnNext(new ProjectionEventContext(new Key("root", indexers), incoming, changeset, ProjectionOperationType.Join, false, "join-source"));
        var joined = changeset.Changes.OfType<Joined>().Single();
        var changed = joined.Changes.OfType<PropertiesChanged<ExpandoObject>>().Single();
        var resultState = (IDictionary<string, object?>)changed.State;
        var count = isChild
            ? Convert.ToInt64(((IDictionary<string, object?>)((IEnumerable<object>)resultState["items"]!).Single())["count"])
            : Convert.ToInt64(resultState["count"]);

        return (((Projection)projection).Subscriptions.Count, count);
    }
}
