// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Projections.Engine;
using Cratis.Chronicle.Projections.Engine.Pipelines;
using Cratis.Chronicle.Projections.Generations;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Sinks;
using Orleans.Core;
using Orleans.TestKit;

using EngineProjection = Cratis.Chronicle.Projections.Engine.IProjection;
using EngineProjectionsManager = Cratis.Chronicle.Projections.Engine.IProjectionsManager;

namespace Cratis.Chronicle.Projections.for_ProjectionObserverSubscriber.given;

/// <summary>
/// A subscriber keyed on a later-generation observer. Nothing is activated until a specification calls
/// <see cref="Activate"/>, so each specification decides what the resolver answers first.
/// </summary>
public class a_generation_bound_subscriber : Specification
{
    protected IGenerationProjectionResolver _generations;
    protected IProjection _projectionGrain;
    protected IReadModel _readModelGrain;
    protected IProjectionChangesetNotifier _notifier;
    protected IProjectionPipelineManager _pipelines;
    protected IGenerationPipelines _generationPipelines;
    protected IProjectionFactory _factory;
    protected EngineProjectionsManager _engineProjections;
    protected Storage.IStorage _storage;
    protected IExpandoObjectConverter _expandoObjectConverter;
    protected ProjectionDefinition _currentDefinition;
    protected ReadModelDefinition _currentReadModel;
    protected EngineProjection _currentProjection;
    protected IProjectionPipeline _currentPipeline;
    protected EngineProjection _generationProjection;
    protected IProjectionPipeline _generationPipeline;
    protected GenerationExecution _execution;
    protected GenerationSinkContext _context;
    protected ObserverSubscriberKey _key;

    void Establish()
    {
        _generationProjection = Substitute.For<EngineProjection>();
        _generationProjection.Accepts(Arg.Any<EventType>()).Returns(true);
        _execution = GenerationExecutions.Create(_generationProjection);
        _context = GenerationExecutions.Context(_execution);
        _key = new(_execution.Binding.Identity.ObserverId, "the-store", GenerationExecutions.Namespace, EventSequenceId.Log, "the-partition", string.Empty);

        _generations = Substitute.For<IGenerationProjectionResolver>();
        _generations.Resolve(_key.EventStore, _key.Namespace, _key.EventSequenceId, _key.ObserverId).Returns(_execution);
        _generations.Admit(_key.EventStore, _execution).Returns(_context);

        _currentDefinition = GenerationExecutions.Definition("currentName");
        _projectionGrain = Substitute.For<IProjection>();
        _projectionGrain.GetDefinition().Returns(_currentDefinition);
        _notifier = Substitute.For<IProjectionChangesetNotifier>();
        _currentReadModel = new(
            _currentDefinition.ReadModel,
            "the-model",
            "The model",
            ReadModelOwner.Client,
            ReadModelSource.Code,
            ReadModelObserverType.Projection,
            ReadModelObserverIdentifier.Unspecified,
            SinkDefinition.None,
            new Dictionary<ReadModelGeneration, JsonSchema> { [ReadModelGeneration.First] = new() },
            []);
        _readModelGrain = Substitute.For<IReadModel>();
        _readModelGrain.GetDefinition().Returns(_currentReadModel);

        _storage = Substitute.For<Storage.IStorage>();
        _storage.GetEventStore(_key.EventStore).EventTypes.GetLatestForAllEventTypes().Returns([]);
        _expandoObjectConverter = Substitute.For<IExpandoObjectConverter>();
        _engineProjections = Substitute.For<EngineProjectionsManager>();

        _currentProjection = Substitute.For<EngineProjection>();
        _currentProjection.Accepts(Arg.Any<EventType>()).Returns(true);
        _factory = Substitute.For<IProjectionFactory>();
        _factory.Create(_key.EventStore, _key.Namespace, Arg.Any<ProjectionDefinition>(), Arg.Any<ReadModelDefinition>(), Arg.Any<IEnumerable<EventTypeSchema>>()).Returns(_currentProjection);

        _currentPipeline = Substitute.For<IProjectionPipeline>();
        _generationPipeline = Substitute.For<IProjectionPipeline>();
        _pipelines = Substitute.For<IProjectionPipelineManager>();
        _pipelines.GetFor(_key.EventStore, _key.Namespace, _currentProjection).Returns(_currentPipeline);
        _generationPipelines = Substitute.For<IGenerationPipelines>();
        _generationPipelines.GetFor(_key.EventStore, _key.Namespace, _generationProjection, Arg.Any<GenerationSinkContext>()).Returns(_generationPipeline);
    }

    /// <summary>
    /// Activates the subscriber on a fresh silo that shares every collaborator with earlier activations.
    /// </summary>
    /// <returns>The activated subscriber.</returns>
    protected async Task<ProjectionObserverSubscriber> Activate()
    {
        var silo = new TestKitSilo();
        silo.Options.StorageFactory = _ => Substitute.For<IStorage<ProjectionDefinition>>();
        silo.AddProbe(_ => _projectionGrain);
        silo.AddProbe(_ => _notifier);
        silo.AddProbe(_ => _readModelGrain);
        silo.AddService(_storage);
        silo.AddService(_expandoObjectConverter);
        silo.AddService(_factory);
        silo.AddService(_engineProjections);
        silo.AddService<IProjectionPipelineManager>(new ForwardingPipelineManager(_pipelines, _generationPipelines));
        silo.AddService(_generations);

        var referenceRuntime = Substitute.For<IGrainReferenceRuntime>();
        referenceRuntime.Cast(Arg.Any<IAddressable>(), typeof(INotifyProjectionDefinitionsChanged))
            .Returns(Substitute.For<INotifyProjectionDefinitionsChanged>());
        var activation = (TestGrainActivationContext)silo.GetOrAddGrainContext<ProjectionObserverSubscriber>(IdSpan.Create(_key.ToString()));
        var sharedReference = new GrainReferenceShared(activation.GrainId.Type, default, 0, referenceRuntime, default, null!, null!, silo.ServiceProvider);
        activation.GrainReference = Substitute.For<GrainReference>(sharedReference, activation.GrainId.Key);
        return await silo.CreateGrainAsync<ProjectionObserverSubscriber>(_key.ToString());
    }
}
