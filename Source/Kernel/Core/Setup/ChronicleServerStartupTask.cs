// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Captures;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.EventSequences;
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
using Microsoft.Extensions.Logging;

using ProjectionDefinitionsRegistrationFailed = Cratis.Chronicle.Projections.Engine.ProjectionDefinitionsRegistrationFailed;

namespace Orleans.Hosting;

/// <summary>
/// Represents a startup task for Chronicle.
/// </summary>
/// <param name="storage"><see cref="IStorage"/> for storing data.</param>
/// <param name="eventTypes"><see cref="IEventTypes"/> for managing kernel event types.</param>
/// <param name="reactors"><see cref="IReactors"/> for managing kernel reactors.</param>
/// <param name="projectionsServiceClient"><see cref="IProjectionsServiceClient"/> for registering projections with local silos.</param>
/// <param name="grainFactory"><see cref="IGrainFactory"/> for creating grains.</param>
/// <param name="authenticationService"><see cref="IAuthenticationService"/> for managing authentication.</param>
/// <param name="logger">The logger.</param>
internal sealed class ChronicleServerStartupTask(
    IStorage storage,
    IEventTypes eventTypes,
    IReactors reactors,
    IProjectionsServiceClient projectionsServiceClient,
    IGrainFactory grainFactory,
    IAuthenticationService authenticationService,
    ILogger<ChronicleServerStartupTask> logger) : ILifecycleParticipant<ISiloLifecycle>
{
    /// <inheritdoc/>
    public void Participate(ISiloLifecycle lifecycle)
    {
        lifecycle.Subscribe(
            nameof(ChronicleServerStartupTask),
            ServiceLifecycleStage.Active,
            Execute);
    }

    async Task Execute(CancellationToken cancellationToken)
    {
        // Apply patches first before anything else starts
        var patchManager = grainFactory.GetGrain<IPatchManager>(0);
        await patchManager.ApplyPatches();

        await grainFactory.GetGrain<INamespaces>(EventStoreName.System).EnsureDefault();

        // Register reactors for the system event store first, so ReactorsReactor can process EventStoreAdded/NamespaceAdded events
        await reactors.DiscoverAndRegister(EventStoreName.System, EventStoreNamespaceName.Default);

        var allEventStores = await storage.GetEventStores();
        foreach (var eventStore in allEventStores)
        {
            await eventTypes.DiscoverAndRegister(eventStore);
            var namespaces = grainFactory.GetGrain<INamespaces>(eventStore);
            await namespaces.EnsureDefault();

            var eventStoreSubscriptionsManager = grainFactory.GetGrain<IEventStoreSubscriptionsManager>(eventStore);
            await eventStoreSubscriptionsManager.Ensure();

            var readModelsManager = grainFactory.GetGrain<IReadModelsManager>(eventStore);
            await readModelsManager.Ensure();

            var projectionsManager = grainFactory.GetGrain<IProjectionsManager>(eventStore);
            await projectionsManager.Ensure();

            var webhooksManager = grainFactory.GetGrain<IWebhooks>(eventStore);
            await webhooksManager.Ensure();

            var capturesManager = grainFactory.GetGrain<ICapturesManager>(eventStore);
            await capturesManager.Ensure();

            var projectionDefinitions = await projectionsManager.GetProjectionDefinitions();
            await RegisterPersistedProjectionDefinitions(eventStore, projectionDefinitions);

            var rehydrateAll = (await namespaces.GetAll()).Select(async namespaceName =>
            {
                await reactors.DiscoverAndRegister(eventStore, namespaceName);

                var jobsManager = grainFactory.GetJobsManager(eventStore, namespaceName);
                await jobsManager.Rehydrate();
                await grainFactory.GetEventSequences(eventStore, namespaceName).Rehydrate();
                await RehydrateReducerAndReactorObservers(eventStore, namespaceName);
            });
            await Task.WhenAll(rehydrateAll);
        }

        await authenticationService.EnsureDefaultAdminUser();
        await authenticationService.EnsureBootstrapClients();
#if DEVELOPMENT
        await authenticationService.EnsureDefaultClientCredentials();
#endif
    }

    async Task RegisterPersistedProjectionDefinitions(EventStoreName eventStore, IEnumerable<ProjectionDefinition> projectionDefinitions)
    {
        try
        {
            await projectionsServiceClient.Register(eventStore, projectionDefinitions);
        }
        catch (Exception exception) when (ProjectionDefinitionsRegistrationFailed.TryFindFailures(exception, out var failures))
        {
            foreach (var (identifier, failure) in failures)
            {
                logger.FailedRegisteringPersistedProjectionDefinition(failure, identifier);
            }
        }
    }

    async Task RehydrateReducerAndReactorObservers(EventStoreName eventStore, EventStoreNamespaceName namespaceName)
    {
        var eventStoreStorage = storage.GetEventStore(eventStore);
        var namespaceStorage = eventStoreStorage.GetNamespace(namespaceName);
        var knownObserverIds = (await namespaceStorage.Observers.GetAll()).Select(_ => _.Identifier).ToHashSet();
        var reducerDefinitions = await eventStoreStorage.Reducers.GetAll();
        var reactorDefinitions = await eventStoreStorage.Reactors.GetAll();

        var reducerObserverKeys = reducerDefinitions
            .Where(_ => knownObserverIds.Contains(_.Identifier))
            .Select(_ => new ObserverKey(_.Identifier, eventStore, namespaceName, _.EventSequenceId));
        var reactorObserverKeys = reactorDefinitions
            .Where(_ => knownObserverIds.Contains(_.Identifier))
            .Select(_ => new ObserverKey(_.Identifier, eventStore, namespaceName, _.EventSequenceId));
        var observerKeys = reducerObserverKeys.Concat(reactorObserverKeys).Distinct().ToArray();

        await Task.WhenAll(observerKeys.Select(_ => grainFactory.GetGrain<IObserver>(_).Ensure()));
    }
}
