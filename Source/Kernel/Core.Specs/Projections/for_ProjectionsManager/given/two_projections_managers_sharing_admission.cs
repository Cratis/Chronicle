// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Namespaces;
using Cratis.Chronicle.Projections.Engine;
using Cratis.Chronicle.Projections.Engine.DeclarationLanguage;
using Cratis.Chronicle.ReadModels;
using Microsoft.Extensions.Logging;
using Orleans.Core;
using Orleans.TestKit;

namespace Cratis.Chronicle.Projections.for_ProjectionsManager.given;

/// <summary>
/// Independent store activations sharing only host admission and membership.
/// </summary>
public class two_projections_managers_sharing_admission : a_projections_manager_grain
{
    protected const string SecondStore = "second-event-store";
    protected TestKitSilo _secondSilo;
    protected ProjectionsManager _secondGrain;
    protected ProjectionsManagerState _secondState;
    protected IProjectionFactory _secondFactory;
    protected Observation.IObserver _secondObserver;
    protected ProjectionDefinition[] _firstDefinitions;
    protected ProjectionDefinition[] _secondDefinitions;
    protected TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);
    protected TaskCompletionSource _eightEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
    protected TaskCompletionSource _subscriptionsEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
    protected TaskCompletionSource _releaseSubscriptions = new(TaskCreationOptions.RunContinuationsAsynchronously);
    protected int _peak;
    protected int _builds;
    protected bool _firstManagerDisposed;
    readonly Lock _buildLock = new();
    int _active;
    int _subscriptionEntries;
    bool _holdSubscriptions;

    async Task Establish()
    {
        _readModelDefinitions = [CreateReadModelDefinition("model")];
        _firstDefinitions = [.. Enumerable.Range(0, 12).Select(index => CreateDefinition($"first-{index}", "model"))];
        _secondDefinitions = [.. Enumerable.Range(0, 12).Select(index => CreateDefinition($"second-{index}", "model"))];
        _secondSilo = new TestKitSilo();
        _secondState = new ProjectionsManagerState();
        var stateStorage = Substitute.For<IStorage<ProjectionsManagerState>>();
        stateStorage.State = _secondState;
        _secondSilo.Options.StorageFactory = _ => stateStorage;
        _secondFactory = Substitute.For<IProjectionFactory>();
        _secondObserver = Substitute.For<Observation.IObserver>();
        _secondObserver.IsSubscriptionEstablished().Returns(true);
        _secondObserver.GetState().Returns(new Storage.Observation.ObserverState());
        _secondGrain = await CreateManager(_secondSilo, SecondStore, _secondFactory, _ => _secondObserver);
    }

    /// <summary>
    /// Creates another activation with the fixture's host gate but its own grain state and probes.
    /// </summary>
    /// <param name="silo">The independent test silo with its state storage configured.</param>
    /// <param name="store">The event store key.</param>
    /// <param name="factory">The store's projection factory.</param>
    /// <param name="observer">Resolves the store's observer probes by grain key.</param>
    /// <returns>The activated manager.</returns>
    protected async Task<ProjectionsManager> CreateManager(TestKitSilo silo, string store, IProjectionFactory factory, Func<string, Observation.IObserver> observer)
    {
        silo.AddService(_throttle);
        silo.AddService(Substitute.For<ILogger<ProjectionsManager>>());
        silo.AddService(_definitionComparer);
        silo.AddService(factory);
        silo.AddService<TimeProvider>(_clock);
        silo.AddService(_projectionsServiceClient);
        silo.AddService(Substitute.For<ILanguageService>());
        silo.AddService(_clusterMembership);
        var details = Substitute.For<ILocalSiloDetails>();
        details.SiloAddress.Returns(SiloAddress.Zero);
        silo.AddService(details);
        var storage = Substitute.For<Storage.IStorage>();
        var eventStore = Substitute.For<Storage.IEventStoreStorage>();
        storage.GetEventStore(Arg.Any<EventStoreName>()).Returns(eventStore);
        eventStore.EventTypes.GetLatestForAllEventTypes().Returns([]);
        silo.AddService(storage);
        var namespaces = Substitute.For<INamespaces>();
        namespaces.GetAll().Returns([EventStoreNamespaceName.Default]);
        silo.AddProbe(_ => namespaces);
        var readModels = Substitute.For<IReadModelsManager>();
        readModels.GetDefinitions().Returns(_ => Task.FromResult(_readModelDefinitions));
        silo.AddProbe(_ => readModels);
        silo.AddProbe(_ => Substitute.For<IProjection>());
        silo.AddProbe(key => observer(key.ToString()));
        return await silo.CreateGrainAsync<ProjectionsManager>(store);
    }

    /// <summary>
    /// Holds admitted builds until explicitly released, recording aggregate activity across both stores.
    /// </summary>
    /// <param name="holdSubscriptions">Whether to hold grants across the remote subscription as well.</param>
    protected void HoldBuilds(bool holdSubscriptions = false)
    {
        _holdSubscriptions = holdSubscriptions;
        _projectionFactory.Create(default!, default!, default!, default!, default!).ReturnsForAnyArgs(_ => Build());
        _secondFactory.Create(default!, default!, default!, default!, default!).ReturnsForAnyArgs(_ => Build());
        if (!holdSubscriptions) return;
        _observerGrain.Subscribe<IProjectionObserverSubscriber>(default, default!, default!).ReturnsForAnyArgs(_ => Subscribe());
        _secondObserver.Subscribe<IProjectionObserverSubscriber>(default, default!, default!).ReturnsForAnyArgs(_ => Subscribe());
    }

    async Task<Engine.IProjection> Build()
    {
        lock (_buildLock)
        {
            _builds++;
            _peak = Math.Max(_peak, ++_active);
            if (_active >= 8) _eightEntered.TrySetResult();
        }

        await _release.Task;
        if (!_holdSubscriptions)
        {
            lock (_buildLock) _active--;
        }

        return _engineProjection;
    }

    async Task Subscribe()
    {
        if (Interlocked.Increment(ref _subscriptionEntries) >= 8) _subscriptionsEntered.TrySetResult();
        await _releaseSubscriptions.Task;
        lock (_buildLock) _active--;
    }

    void Destroy()
    {
        _release.TrySetResult();
        _releaseSubscriptions.TrySetResult();
        if (!_firstManagerDisposed) _grain?.Dispose();
        _secondGrain?.Dispose();
    }
}
