// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections.Engine;
using Cratis.Chronicle.Projections.Engine.Expressions;
using Cratis.Chronicle.Projections.Engine.Expressions.EventValues;
using Cratis.Chronicle.Projections.Engine.Expressions.Keys;
using Cratis.Chronicle.Projections.Engine.Pipelines;
using Cratis.Chronicle.Projections.Engine.Pipelines.Steps;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Changes;
using Cratis.Chronicle.Storage.EventSequences;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_applying_changes_guarded_on_watermark;

[Collection(MongoDBCollection.Name)]
public class and_a_pinned_count_event_is_redelivered(MongoDBFixture fixture) : given.an_accumulating_read_model(fixture)
{
    ProjectionPipeline _pipeline;
    IProjection _projection;
    AppendedEvent _event;
    int _count;

    async Task Establish()
    {
        var first = new EventType("Counted", 1);
        var definition = new ProjectionDefinition(
            ProjectionOwner.Client,
            EventSequenceId.Log,
            "counter",
            _readModel.Identifier,
            true,
            true,
            new(),
            new Dictionary<EventType, FromDefinition>
            {
                [first] = new(new Dictionary<PropertyPath, string> { [(PropertyPath)"count"] = WellKnownExpressions.Count }, PropertyExpression.NotSet, null)
            },
            new Dictionary<EventType, JoinDefinition>(),
            new Dictionary<PropertyPath, ChildrenDefinition>(),
            [],
            new(new Dictionary<PropertyPath, string>(), false),
            new Dictionary<EventType, RemovedWithDefinition>(),
            new Dictionary<EventType, RemovedWithJoinDefinition>(),
            AutoMap: AutoMap.Disabled)
        { GenerationDelivery = EventGenerationDelivery.Pinned };
        var formats = new TypeFormats();
        var keys = new KeyResolvers(NullLogger<KeyResolvers>.Instance);
        var values = new EventValueProviderExpressionResolvers(formats, NullLogger<EventValueProviderExpressionResolvers>.Instance);
        var sequence = Substitute.For<IEventSequenceStorage>();
        var storage = Substitute.For<IStorage>();
        storage.GetEventStore("store").GetNamespace("namespace").GetEventSequence(EventSequenceId.Log).Returns(sequence);
        var factory = new ProjectionFactory(
            new ReadModelPropertyExpressionResolvers(values, formats, NullLogger<ReadModelPropertyExpressionResolvers>.Instance),
            values,
            new KeyExpressionResolvers(values, keys, NullLogger<KeyExpressionResolvers>.Instance),
            new Json.ExpandoObjectConverter(formats),
            keys,
            storage,
            NullLogger<ProjectionFactory>.Instance,
            eventGenerationRelease: Substitute.For<IEventGenerationRelease>());
        var schema = JsonSchema.FromType<ExpandoObject>();
        _projection = await factory.Create("store", "namespace", definition, _readModel, [new(first, EventTypeOwner.Client, EventTypeSource.Code, schema)]);
        var changes = Substitute.For<IChangesetStorage>();
        ICanPerformProjectionPipelineStep[] steps =
        [
            new ResolveKey(sequence, _sink, formats, NullLogger<ResolveKey>.Instance),
            new SetInitialState(_sink, NullLogger<SetInitialState>.Instance),
            new HandleEvent(sequence, _sink, NullLogger<HandleEvent>.Instance),
            new SaveChanges(_sink, changes, true, NullLogger<SaveChanges>.Instance)
        ];
        _pipeline = new(_projection, _sink, changes, new ObjectComparer(), steps, new ProjectionHandleLock(), Substitute.For<IReplayScopedCache>(), NullLogger<ProjectionPipeline>.Instance);
        _event = new(new EventContext(first, EventSourceType.Default, "counter-1", EventStreamType.All, EventStreamId.Default, 10, DateTimeOffset.UtcNow, "store", "namespace", CorrelationId.New(), [], Identity.System, [], EventHash.NotSet), new ExpandoObject());
        await _pipeline.Handle(_event);
    }

    async Task Because()
    {
        await _pipeline.Handle(_event);
        _count = await CurrentCount();
    }

    [Fact] void should_be_event_source_keyed() => _projection.IsEventSourceKeyed.ShouldBeTrue();
    [Fact] void should_count_the_event_only_once() => _count.ShouldEqual(1);

    void Destroy() => (_projection as IDisposable)?.Dispose();
}
