// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Projections.Engine;
using Cratis.Chronicle.Projections.Engine.Pipelines;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Schemas;
using Orleans.Core;
using Orleans.TestKit;

using EngineProjection = Cratis.Chronicle.Projections.Engine.IProjection;
using EngineProjectionsManager = Cratis.Chronicle.Projections.Engine.IProjectionsManager;
using KernelProjection = Cratis.Chronicle.Projections.IProjection;

namespace Cratis.Chronicle.Services.ReadModels.for_ReadModels.when_watching.across_scopes.given;

/// <summary>
/// A real <see cref="ProjectionObserverSubscriber"/> for one event store and namespace, whose notifier lookups are
/// routed through a <see cref="NotifierRouter"/> and whose pipeline produces one change per delivered page.
/// </summary>
public class ProjectionProducer
{
    ProjectionObserverSubscriber _subscriber = null!;
    ObserverSubscriberKey _key = null!;
    long _sequence;

    public static async Task<ProjectionProducer> Create(NotifierRouter router, ObserverId observerId, EventStoreName eventStore, EventStoreNamespaceName @namespace)
    {
        var producer = new ProjectionProducer
        {
            _key = new ObserverSubscriberKey(observerId, eventStore, @namespace, EventSequenceId.Log, "the-partition", string.Empty)
        };
        await producer.Initialize(router);
        return producer;
    }

    /// <summary>
    /// Delivers one page with one event; the resulting change carries the value.
    /// </summary>
    /// <param name="value">The value the resulting change carries.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task Produce(string value)
    {
        var sequence = (ulong)Interlocked.Increment(ref _sequence);
        var @event = AppendedEvent.EmptyWithEventSequenceNumber(sequence) with
        {
            Context = EventContext.Empty with { SequenceNumber = sequence, EventSourceId = value }
        };
        var result = await _subscriber.OnNextPage([@event], new(null));
        result.State.ShouldEqual(ObserverSubscriberState.Ok);
    }

    static ProjectionDefinition CreateDefinition(ObserverId observerId) => new(
        ProjectionOwner.Client,
        EventSequenceId.Log,
        observerId.Value,
        "the-model",
        true,
        true,
        new JsonObject(),
        new Dictionary<EventType, FromDefinition>(),
        new Dictionary<EventType, JoinDefinition>(),
        new Dictionary<PropertyPath, ChildrenDefinition>(),
        [],
        new FromEveryDefinition(new Dictionary<PropertyPath, string>(), false),
        new Dictionary<EventType, RemovedWithDefinition>(),
        new Dictionary<EventType, RemovedWithJoinDefinition>());

    async Task Initialize(NotifierRouter router)
    {
        var silo = new TestKitSilo();
        var definition = CreateDefinition(_key.ObserverId);
        var stateStorage = Substitute.For<IStorage<ProjectionDefinition>>();
        stateStorage.State = definition;
        silo.Options.StorageFactory = _ => stateStorage;

        var projectionGrain = Substitute.For<KernelProjection>();
        projectionGrain.GetDefinition().Returns(definition);
        silo.AddProbe(_ => projectionGrain);
        silo.AddProbe(_ => Substitute.For<IObserver>());
        silo.AddProbe(identity => router.For(identity.ToString()));

        var readModel = new ReadModelDefinition(
            definition.ReadModel,
            "the-model",
            "The model",
            ReadModelOwner.Client,
            ReadModelSource.Code,
            ReadModelObserverType.Projection,
            ReadModelObserverIdentifier.Unspecified,
            SinkDefinition.None,
            new Dictionary<ReadModelGeneration, JsonSchema> { [ReadModelGeneration.First] = new() },
            []);
        var readModelGrain = Substitute.For<IReadModel>();
        readModelGrain.GetDefinition().Returns(readModel);
        silo.AddProbe(_ => readModelGrain);

        var storage = Substitute.For<Storage.IStorage>();
        storage.GetEventStore(_key.EventStore).EventTypes.GetLatestForAllEventTypes().Returns([]);
        storage.GetEventStore(_key.EventStore).Projections.Has(_key.ObserverId).Returns(true);
        storage.GetEventStore(_key.EventStore).Projections.Get(_key.ObserverId).Returns(definition);
        silo.AddService(storage);

        var expandoObjectConverter = silo.AddService(Substitute.For<IExpandoObjectConverter>());
        expandoObjectConverter.ToJsonObject(Arg.Any<ExpandoObject>(), Arg.Any<JsonSchema>()).Returns(call =>
            new JsonObject { ["value"] = JsonValue.Create((string?)((IDictionary<string, object?>)call.Arg<ExpandoObject>())["value"]) });

        var projection = Substitute.For<EngineProjection>();
        projection.Accepts(Arg.Any<EventType>()).Returns(true);
        var factory = Substitute.For<IProjectionFactory>();
        factory.Create(_key.EventStore, _key.Namespace, Arg.Any<ProjectionDefinition>(), readModel, Arg.Any<IEnumerable<EventTypeSchema>>()).Returns(projection);
        silo.AddService(factory);
        silo.AddService(Substitute.For<EngineProjectionsManager>());

        var pipeline = Substitute.For<IProjectionPipeline>();
        pipeline.Handle(Arg.Any<AppendedEvent>()).Returns(call =>
        {
            var @event = call.Arg<AppendedEvent>();
            var changeset = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
            changeset.HasChanges.Returns(true);
            changeset.HasBeenRemoved().Returns(false);
            var state = new ExpandoObject();
            ((IDictionary<string, object?>)state)["value"] = @event.Context.EventSourceId.Value;
            changeset.CurrentState.Returns(state);
            return new ProjectionEventContext(@event.Context.EventSourceId, @event, changeset, ProjectionOperationType.From, false)
            {
                IsNewInstance = true
            };
        });
        var pipelines = Substitute.For<IProjectionPipelineManager>();
        pipelines.GetFor(_key.EventStore, _key.Namespace, projection).Returns(pipeline);
        silo.AddService(pipelines);

        var referenceRuntime = Substitute.For<IGrainReferenceRuntime>();
        referenceRuntime.Cast(Arg.Any<IAddressable>(), typeof(INotifyProjectionDefinitionsChanged))
            .Returns(Substitute.For<INotifyProjectionDefinitionsChanged>());
        var activation = (TestGrainActivationContext)silo.GetOrAddGrainContext<ProjectionObserverSubscriber>(IdSpan.Create(_key.ToString()));
        var sharedReference = new GrainReferenceShared(activation.GrainId.Type, default, 0, referenceRuntime, default, null!, null!, silo.ServiceProvider);
        activation.GrainReference = Substitute.For<GrainReference>(sharedReference, activation.GrainId.Key);
        _subscriber = await silo.CreateGrainAsync<ProjectionObserverSubscriber>(_key.ToString());
    }
}
