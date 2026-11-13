// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Dynamic;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Projections.Engine.Expressions;
using Cratis.Chronicle.Projections.Engine.Expressions.EventValues;
using Cratis.Chronicle.Projections.Engine.Expressions.Keys;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Changes;
using Cratis.Chronicle.Storage.Identities;
using Cratis.Chronicle.Storage.InMemory.EventSequences;
using Cratis.Chronicle.Storage.InMemory.Sinks;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.Steps.for_ResolveFutures.given;

public class two_deferred_events_for_one_child : Specification
{
    protected static readonly EventType _childAdded = "ChildAddedToRoot";
    protected static readonly EventType _emailChanged = "ChildEmailChanged";
    protected static readonly EventType _rootCreated = "RootCreated";
    protected IProjection _projection;
    protected InMemorySink _sink;
    protected IProjectionFuturesStore _futures;
    protected ResolveFutures _resolve;
    protected SaveChanges _save;
    protected ICanPerformProjectionPipelineStep[] _beforeResolve;

    async Task Establish()
    {
        EventStoreName eventStore = "event-store";
        EventStoreNamespaceName @namespace = "namespace";
        var sequence = new EventSequenceStorage(eventStore, @namespace, EventSequenceId.Log, Substitute.For<IIdentityStorage>());
        var storage = Substitute.For<IStorage>();
        var eventStoreStorage = Substitute.For<IEventStoreStorage>();
        var namespaceStorage = Substitute.For<IEventStoreNamespaceStorage>();
        storage.GetEventStore(eventStore).Returns(eventStoreStorage);
        eventStoreStorage.GetNamespace(@namespace).Returns(namespaceStorage);
        namespaceStorage.GetEventSequence(EventSequenceId.Log).Returns(sequence);

        var formats = new TypeFormats();
        var keyResolvers = new KeyResolvers(NullLogger<KeyResolvers>.Instance);
        var values = new EventValueProviderExpressionResolvers(formats, NullLogger<EventValueProviderExpressionResolvers>.Instance);
        var factory = new ProjectionFactory(
            new ReadModelPropertyExpressionResolvers(values, formats, NullLogger<ReadModelPropertyExpressionResolvers>.Instance),
            values,
            new KeyExpressionResolvers(values, keyResolvers, NullLogger<KeyExpressionResolvers>.Instance),
            new ExpandoObjectConverter(formats),
            keyResolvers,
            storage,
            NullLogger<ProjectionFactory>.Instance);
        var schema = await JsonSchema.FromJsonAsync("""
            {"type":"object","properties":{
              "id":{"type":"string"},"name":{"type":"string"},
              "children":{"type":"array","items":{"type":"object","properties":{
                "id":{"type":"string"},"name":{"type":"string"},"email":{"type":"string"}
              }}}
            }}
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
        _projection = await factory.Create(eventStore, @namespace, Definition(), readModel, []);
        _sink = new InMemorySink(readModel, formats);
        _futures = new PartitionRebuildFutures();
        var tracker = new ProjectionFuturesTracker();
        _resolve = new ResolveFutures(
            tracker,
            _futures,
            formats,
            new ObjectComparer(),
            Substitute.For<ICanProtectFutureChangesets>(),
            NullLogger<ResolveFutures>.Instance,
            sequence,
            _sink);
        _save = new SaveChanges(_futures, _sink, Substitute.For<IChangesetStorage>(), false, NullLogger<SaveChanges>.Instance);
        _beforeResolve =
        [
            new ResolveKey(sequence, _sink, formats, NullLogger<ResolveKey>.Instance),
            new SetInitialState(_sink, NullLogger<SetInitialState>.Instance),
            new HandleEvent(sequence, _sink, NullLogger<HandleEvent>.Instance),
            new StoreFutures(tracker, _futures, NullLogger<StoreFutures>.Instance)
        ];
    }

    void Destroy() => _sink.Dispose();

    protected static AppendedEvent Event(EventType type, ulong sequence, object content) => new(
        EventContext.EmptyWithEventSourceId("root") with { EventType = type, SequenceNumber = sequence },
        content.AsExpandoObject());

    static ProjectionDefinition Definition()
    {
        var children = new ChildrenDefinition(
            (PropertyPath)"id",
            new Dictionary<EventType, FromDefinition>
            {
                [_childAdded] = new(new Dictionary<PropertyPath, string> { ["name"] = "name", ["email"] = "email" }, "id", WellKnownExpressions.EventSourceId),
                [_emailChanged] = new(new Dictionary<PropertyPath, string> { ["email"] = "email" }, "id", WellKnownExpressions.EventSourceId)
            },
            new Dictionary<EventType, JoinDefinition>(),
            new Dictionary<PropertyPath, ChildrenDefinition>(),
            new FromEveryDefinition(new Dictionary<PropertyPath, string>(), false),
            new Dictionary<EventType, RemovedWithDefinition>(),
            new Dictionary<EventType, RemovedWithJoinDefinition>(),
            AutoMap: AutoMap.Disabled);
        return new ProjectionDefinition(
            ProjectionOwner.Client,
            EventSequenceId.Log,
            "test-projection",
            "test-model",
            true,
            true,
            new(),
            new Dictionary<EventType, FromDefinition>
            {
                [_rootCreated] = new(new Dictionary<PropertyPath, string> { ["name"] = "name" }, PropertyExpression.NotSet, null)
            },
            new Dictionary<EventType, JoinDefinition>(),
            new Dictionary<PropertyPath, ChildrenDefinition> { ["children"] = children },
            [],
            new FromEveryDefinition(new Dictionary<PropertyPath, string>(), false),
            new Dictionary<EventType, RemovedWithDefinition>(),
            new Dictionary<EventType, RemovedWithJoinDefinition>(),
            AutoMap: AutoMap.Disabled);
    }
}
