// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Projections.Engine;
using Cratis.Chronicle.Projections.Engine.Pipelines;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Schemas;
using Orleans.Core;
using Orleans.TestKit;

using EngineProjection = Cratis.Chronicle.Projections.Engine.IProjection;

namespace Cratis.Chronicle.Projections.for_ProjectionObserverSubscriber.given;

public class a_subscriber_with_a_cached_pipeline : Specification
{
    protected ProjectionObserverSubscriber _subscriber;
    protected ProjectionDefinition _definition;
    protected IProjectionPipelineManager _pipelines;
    protected IProjectionFactory _factory;
    protected EngineProjection _projection;
    protected IProjectionPipeline _cachedPipeline;
    protected IProjectionPipeline _refreshedPipeline;
    protected IProjectionPipeline _returnedPipeline;
    protected readonly ObserverSubscriberKey _key = new("the-projection", "the-store", "the-namespace", EventSequenceId.Log, "the-partition", string.Empty);

    async Task Establish()
    {
        var silo = new TestKitSilo();
        _definition = CreateDefinition();
        var stateStorage = Substitute.For<IStorage<ProjectionDefinition>>();
        stateStorage.State = _definition;
        silo.Options.StorageFactory = _ => stateStorage;

        var projectionGrain = Substitute.For<IProjection>();
        projectionGrain.GetDefinition().Returns(_definition);
        silo.AddProbe(_ => projectionGrain);
        silo.AddProbe(_ => Substitute.For<IProjectionChangesetNotifier>());

        var readModel = new ReadModelDefinition(
            _definition.ReadModel,
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
        silo.AddService(storage);
        silo.AddService(Substitute.For<IExpandoObjectConverter>());
        _factory = Substitute.For<IProjectionFactory>();
        _projection = Substitute.For<EngineProjection>();

        // The subscriber skips an event the current definition does not take part in, so a substitute left at
        // its default of false would silently skip everything. Specifications about skipping override this.
        _projection.Accepts(Arg.Any<EventType>()).Returns(true);
        _factory.Create(_key.EventStore, _key.Namespace, Arg.Any<ProjectionDefinition>(), readModel, Arg.Any<IEnumerable<EventTypeSchema>>()).Returns(_projection);
        silo.AddService(_factory);

        _cachedPipeline = Substitute.For<IProjectionPipeline>();
        _refreshedPipeline = Substitute.For<IProjectionPipeline>();
        _pipelines = Substitute.For<IProjectionPipelineManager>();
        var cached = true;
        _pipelines.When(_ => _.EvictFor(_key.EventStore, _key.Namespace, _key.ObserverId)).Do(_ => cached = false);
        _pipelines.GetFor(_key.EventStore, _key.Namespace, _projection).Returns(_ =>
        {
            _returnedPipeline = cached ? _cachedPipeline : _refreshedPipeline;
            return _returnedPipeline;
        });
        silo.AddService(_pipelines);

        var referenceRuntime = Substitute.For<IGrainReferenceRuntime>();
        referenceRuntime.Cast(Arg.Any<IAddressable>(), typeof(INotifyProjectionDefinitionsChanged))
            .Returns(Substitute.For<INotifyProjectionDefinitionsChanged>());
        var activation = (TestGrainActivationContext)silo.GetOrAddGrainContext<ProjectionObserverSubscriber>(IdSpan.Create(_key.ToString()));
        var sharedReference = new GrainReferenceShared(activation.GrainId.Type, default, 0, referenceRuntime, default, null!, null!, silo.ServiceProvider);
        activation.GrainReference = Substitute.For<GrainReference>(sharedReference, activation.GrainId.Key);
        _subscriber = await silo.CreateGrainAsync<ProjectionObserverSubscriber>(_key.ToString());
        _pipelines.ClearReceivedCalls();
        _factory.ClearReceivedCalls();
    }

    static ProjectionDefinition CreateDefinition() => new(
        ProjectionOwner.Client,
        EventSequenceId.Log,
        "the-projection",
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
}
