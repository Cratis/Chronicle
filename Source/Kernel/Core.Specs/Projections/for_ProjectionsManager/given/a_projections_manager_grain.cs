// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Namespaces;
using Cratis.Chronicle.Projections.Engine;
using Cratis.Chronicle.Projections.Engine.DeclarationLanguage;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Observation;
using Microsoft.Extensions.Logging;
using Orleans.Core;
using Orleans.TestKit;

namespace Cratis.Chronicle.Projections.for_ProjectionsManager.given;

public class a_projections_manager_grain : Specification
{
    protected const string EventStore = "the-event-store";

    protected ProjectionsManager _grain;
    protected ILogger<ProjectionsManager> _logger;
    protected TestKitSilo _silo;
    protected IProjectionsServiceClient _projectionsServiceClient;
    protected IProjectionDefinitionComparer _definitionComparer;
    protected IProjection _projectionGrain;
    protected IProjectionFactory _projectionFactory;
    protected Engine.IProjection _engineProjection;
    protected AdvancingClock _clock;
    protected IReadModelsManager _readModelsManager;
    protected Observation.IObserver _observerGrain;
    protected readonly Dictionary<string, Observation.IObserver> _observersByKey = [];
    protected INamespaces _namespaces;
    protected Cratis.Orleans.Jobs.IJobsManager _jobsManager;
    protected Recommendations.IRecommendationsManager _recommendationsManager;
    protected Storage.Observation.IFailedPartitionsStorage _failedPartitionsStorage;
    protected Storage.IEventStoreStorage _eventStoreStorage;
    protected ProjectionsManagerState _state;
    protected IStorage<ProjectionsManagerState> _stateStorage;
    protected Storage.Observation.IObserverStateStorage _observerStates;
    protected IEnumerable<ReadModelDefinition> _readModelDefinitions = [];

    async Task Establish()
    {
        _silo = new TestKitSilo();
        _logger = Substitute.For<ILogger<ProjectionsManager>>();
        _logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);
        _silo.AddService(_logger);

        _definitionComparer = Substitute.For<IProjectionDefinitionComparer>();
        _silo.AddService(_definitionComparer);

        var engineProjection = _engineProjection = Substitute.For<Engine.IProjection>();
        engineProjection.EventTypes.Returns([]);
        engineProjection.IsEventSourceKeyed.Returns(true);
        var projectionFactory = _projectionFactory = Substitute.For<IProjectionFactory>();
        projectionFactory
            .Create(
                Arg.Any<EventStoreName>(),
                Arg.Any<EventStoreNamespaceName>(),
                Arg.Any<ProjectionDefinition>(),
                Arg.Any<ReadModelDefinition>(),
                Arg.Any<IEnumerable<EventTypeSchema>>())
            .Returns(engineProjection);
        _silo.AddService(projectionFactory);

        _clock = new AdvancingClock();
        _silo.AddService<TimeProvider>(_clock);

        _projectionsServiceClient = Substitute.For<IProjectionsServiceClient>();
        _projectionsServiceClient
            .Register(Arg.Any<EventStoreName>(), Arg.Any<IEnumerable<ProjectionDefinition>>())
            .Returns(Task.FromResult(Cratis.Monads.Result<ProjectionRegistrationError>.Success()));
        _silo.AddService(_projectionsServiceClient);

        _silo.AddService(Substitute.For<ILanguageService>());

        var storage = Substitute.For<Storage.IStorage>();
        var eventStoreStorage = _eventStoreStorage = Substitute.For<Storage.IEventStoreStorage>();
        storage.GetEventStore(Arg.Any<EventStoreName>()).Returns(eventStoreStorage);
        eventStoreStorage.EventTypes.GetLatestForAllEventTypes().Returns([]);
        var namespaceStorage = Substitute.For<Storage.IEventStoreNamespaceStorage>();
        eventStoreStorage.GetNamespace(Arg.Any<EventStoreNamespaceName>()).Returns(namespaceStorage);
        _failedPartitionsStorage = Substitute.For<Storage.Observation.IFailedPartitionsStorage>();
        namespaceStorage.FailedPartitions.Returns(_failedPartitionsStorage);
        _observerStates = namespaceStorage.Observers;
        _silo.AddService(storage);

        var siloDetails = Substitute.For<ILocalSiloDetails>();
        siloDetails.SiloAddress.Returns(SiloAddress.Zero);
        _silo.AddService(siloDetails);

        _namespaces = Substitute.For<INamespaces>();
        _namespaces.GetAll().Returns([EventStoreNamespaceName.Default]);
        _silo.AddProbe(_ => _namespaces);

        _readModelsManager = Substitute.For<IReadModelsManager>();
        _readModelsManager.GetDefinitions().Returns(_ => Task.FromResult(_readModelDefinitions));
        _silo.AddProbe(_ => _readModelsManager);

        _projectionGrain = Substitute.For<IProjection>();
        _silo.AddProbe(_ => _projectionGrain);

        _observerGrain = Substitute.For<Observation.IObserver>();
        _observerGrain.IsSubscribed().Returns(true);
        _observerGrain.IsSubscriptionEstablished().Returns(true);
        _observerGrain.GetState().Returns(new ObserverState());
        _silo.AddProbe<Observation.IObserver>(key => _observersByKey.TryGetValue(key.ToString(), out var observer) ? observer : _observerGrain);

        _jobsManager = Substitute.For<Cratis.Orleans.Jobs.IJobsManager>();
        _jobsManager.GetAllJobs().Returns(System.Collections.Immutable.ImmutableList<Cratis.Orleans.Storage.Jobs.JobState>.Empty);
        _silo.AddProbe(_ => _jobsManager);

        _recommendationsManager = Substitute.For<Recommendations.IRecommendationsManager>();
        _silo.AddProbe(_ => _recommendationsManager);

        _state = new ProjectionsManagerState();
        _stateStorage = Substitute.For<IStorage<ProjectionsManagerState>>();
        _stateStorage.State = _state;
        _silo.Options.StorageFactory = _ => _stateStorage;

        _grain = await _silo.CreateGrainAsync<ProjectionsManager>(EventStore);
    }

    internal static ProjectionDefinition CreateDefinition(ProjectionId identifier, ReadModelIdentifier readModel) => new(
        ProjectionOwner.Client,
        EventSequenceId.Log,
        identifier,
        readModel,
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

    internal static ReadModelDefinition CreateReadModelDefinition(ReadModelIdentifier identifier, string? containerName = null) => new(
        identifier,
        containerName ?? "TheReadModel",
        "TheReadModel",
        ReadModelOwner.Client,
        ReadModelSource.Code,
        ReadModelObserverType.Projection,
        ReadModelObserverIdentifier.Unspecified,
        SinkDefinition.None,
        new Dictionary<ReadModelGeneration, JsonSchema>(),
        []);
}
