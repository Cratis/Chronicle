// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.EventTypes;
using Cratis.Chronicle.Jobs;
using Cratis.Chronicle.Namespaces;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Observation.EventStoreSubscriptions;
using Cratis.Chronicle.Observation.Reactors.Kernel;
using Cratis.Chronicle.Observation.Webhooks;
using Cratis.Chronicle.Patching;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Setup.Authentication;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Observation;
using Cratis.Chronicle.Storage.Observation.Reactors;
using Cratis.Chronicle.Storage.Observation.Reducers;

namespace Orleans.Hosting.for_ChronicleServerStartupTask.given;

public class a_startup_task : Specification
{
    ChronicleServerStartupTask _startupTask = null!;
    protected IStorage _storage = null!;
    protected IEventTypes _eventTypes = null!;
    protected IReactors _reactors = null!;
    protected IProjectionsServiceClient _projectionsServiceClient = null!;
    protected IGrainFactory _grainFactory = null!;
    IAuthenticationService _authenticationService = null!;
    protected IEventStoreStorage _eventStoreStorage = null!;
    protected IEventStoreNamespaceStorage _namespaceStorage = null!;
    protected IObserverStateStorage _observerStateStorage = null!;
    protected IReactorDefinitionsStorage _reactorDefinitionsStorage = null!;
    protected IReducerDefinitionsStorage _reducerDefinitionsStorage = null!;
    protected IPatchManager _patchManager = null!;
    protected INamespaces _systemNamespaces = null!;
    protected INamespaces _namespaces = null!;
    protected IEventStoreSubscriptionsManager _eventStoreSubscriptionsManager = null!;
    protected IReadModelsManager _readModelsManager = null!;
    protected IProjectionsManager _projectionsManager = null!;
    protected IWebhooks _webhooksManager = null!;
    protected IJobsManager _jobsManager = null!;
    protected IObserver _reducerObserver = null!;
    protected IObserver _reactorObserver = null!;
    protected EventStoreName _eventStore;
    protected EventStoreNamespaceName _namespace;
    protected ObserverKey _reducerObserverKey = ObserverKey.NotSet;
    protected ObserverKey _reactorObserverKey = ObserverKey.NotSet;

    void Establish()
    {
        _storage = Substitute.For<IStorage>();
        _eventTypes = Substitute.For<IEventTypes>();
        _reactors = Substitute.For<IReactors>();
        _projectionsServiceClient = Substitute.For<IProjectionsServiceClient>();
        _grainFactory = Substitute.For<IGrainFactory>();
        _authenticationService = new TestAuthenticationService();
        _eventStoreStorage = Substitute.For<IEventStoreStorage>();
        _namespaceStorage = Substitute.For<IEventStoreNamespaceStorage>();
        _observerStateStorage = Substitute.For<IObserverStateStorage>();
        _reactorDefinitionsStorage = Substitute.For<IReactorDefinitionsStorage>();
        _reducerDefinitionsStorage = Substitute.For<IReducerDefinitionsStorage>();
        _patchManager = Substitute.For<IPatchManager>();
        _systemNamespaces = Substitute.For<INamespaces>();
        _namespaces = Substitute.For<INamespaces>();
        _eventStoreSubscriptionsManager = Substitute.For<IEventStoreSubscriptionsManager>();
        _readModelsManager = Substitute.For<IReadModelsManager>();
        _projectionsManager = Substitute.For<IProjectionsManager>();
        _webhooksManager = Substitute.For<IWebhooks>();
        _jobsManager = Substitute.For<IJobsManager>();
        _reducerObserver = Substitute.For<IObserver>();
        _reactorObserver = Substitute.For<IObserver>();

        _eventStore = "event-store";
        _namespace = "namespace";
        _reducerObserverKey = new ObserverKey("reducer", _eventStore, _namespace, EventSequenceId.Log);
        _reactorObserverKey = new ObserverKey("reactor", _eventStore, _namespace, EventSequenceId.Log);

        _startupTask = new(
            _storage,
            _eventTypes,
            _reactors,
            _projectionsServiceClient,
            _grainFactory,
            _authenticationService);

        _storage.GetEventStores().Returns(Task.FromResult<IEnumerable<EventStoreName>>([_eventStore]));
        _storage.GetEventStore(_eventStore).Returns(_eventStoreStorage);

        _eventStoreStorage.GetNamespace(_namespace).Returns(_namespaceStorage);
        _eventStoreStorage.Reactors.Returns(_reactorDefinitionsStorage);
        _eventStoreStorage.Reducers.Returns(_reducerDefinitionsStorage);
        _namespaceStorage.Observers.Returns(_observerStateStorage);

        _patchManager.ApplyPatches().Returns(Task.CompletedTask);
        _systemNamespaces.EnsureDefault().Returns(Task.CompletedTask);
        _namespaces.EnsureDefault().Returns(Task.CompletedTask);
        _eventStoreSubscriptionsManager.Ensure().Returns(Task.CompletedTask);
        _readModelsManager.Ensure().Returns(Task.CompletedTask);
        _projectionsManager.Ensure().Returns(Task.CompletedTask);
        _projectionsManager.GetProjectionDefinitions().Returns(Task.FromResult<IEnumerable<ProjectionDefinition>>([]));
        _webhooksManager.Ensure().Returns(Task.CompletedTask);
        _jobsManager.Rehydrate().Returns(Task.CompletedTask);
        _reducerObserver.Ensure().Returns(Task.CompletedTask);
        _reactorObserver.Ensure().Returns(Task.CompletedTask);
        _eventTypes.DiscoverAndRegister(_eventStore).Returns(Task.CompletedTask);
        _reactors.DiscoverAndRegister(EventStoreName.System, EventStoreNamespaceName.Default).Returns(Task.CompletedTask);
        _reactors.DiscoverAndRegister(_eventStore, _namespace).Returns(Task.CompletedTask);
        _projectionsServiceClient.Register(_eventStore, Arg.Any<IEnumerable<ProjectionDefinition>>()).Returns(Task.CompletedTask);

        _grainFactory.GetGrain<IPatchManager>(0).Returns(_patchManager);
        _grainFactory.GetGrain<INamespaces>(EventStoreName.System).Returns(_systemNamespaces);
        _grainFactory.GetGrain<INamespaces>(_eventStore).Returns(_namespaces);
        _grainFactory.GetGrain<IEventStoreSubscriptionsManager>(_eventStore).Returns(_eventStoreSubscriptionsManager);
        _grainFactory.GetGrain<IReadModelsManager>(_eventStore).Returns(_readModelsManager);
        _grainFactory.GetGrain<IProjectionsManager>(_eventStore).Returns(_projectionsManager);
        _grainFactory.GetGrain<IWebhooks>(_eventStore).Returns(_webhooksManager);
        _grainFactory.GetGrain<IJobsManager>(0, new JobsManagerKey(_eventStore, _namespace)).Returns(_jobsManager);
        _grainFactory.GetGrain<IObserver>(_reducerObserverKey).Returns(_reducerObserver);
        _grainFactory.GetGrain<IObserver>(_reactorObserverKey).Returns(_reactorObserver);
        _namespaces.GetAll().Returns(Task.FromResult<IEnumerable<EventStoreNamespaceName>>([_namespace]));
    }

    protected Task Execute()
    {
        var execute = typeof(ChronicleServerStartupTask).GetMethod("Execute", BindingFlags.Instance | BindingFlags.NonPublic)!;
        return (Task)execute.Invoke(_startupTask, [CancellationToken.None])!;
    }

    class TestAuthenticationService : IAuthenticationService
    {
        public Task<Cratis.Chronicle.Storage.Security.User?> AuthenticateUser(
            Cratis.Chronicle.Concepts.Security.Username username,
            Cratis.Chronicle.Concepts.Security.Password password) => Task.FromResult<Cratis.Chronicle.Storage.Security.User?>(null);

        public Task EnsureDefaultAdminUser() => Task.CompletedTask;

        public Task EnsureBootstrapClients() => Task.CompletedTask;
#if DEVELOPMENT
        public Task EnsureDefaultClientCredentials() => Task.CompletedTask;
#endif
    }
}
