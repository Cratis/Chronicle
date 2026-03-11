// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.EventTypes;
using Cratis.Chronicle.Namespaces;
using Cratis.Chronicle.Observation.Reactors.Kernel;
using Cratis.Chronicle.Observation.Webhooks;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Setup.Authentication;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Setup;

/// <summary>
/// Represents an implementation of <see cref="IChronicleInitializer"/>.
/// </summary>
/// <param name="storage"><see cref="IStorage"/> for storing data.</param>
/// <param name="eventTypes"><see cref="IEventTypes"/> for managing kernel event types.</param>
/// <param name="reactors"><see cref="IReactors"/> for managing kernel reactors.</param>
/// <param name="projectionsServiceClient"><see cref="IProjectionsServiceClient"/> for registering projections with local silos.</param>
/// <param name="grainFactory"><see cref="IGrainFactory"/> for creating grains.</param>
/// <param name="authenticationService"><see cref="IAuthenticationService"/> for managing authentication.</param>
internal sealed class ChronicleInitializer(
    IStorage storage,
    IEventTypes eventTypes,
    IReactors reactors,
    IProjectionsServiceClient projectionsServiceClient,
    IGrainFactory grainFactory,
    IAuthenticationService authenticationService) : IChronicleInitializer
{
    /// <inheritdoc/>
    public async Task Initialize(CancellationToken cancellationToken = default)
    {
        await grainFactory.GetGrain<INamespaces>(EventStoreName.System).EnsureDefault();

        // Register System reactors first so ReactorsReactor can process EventStoreAdded/NamespaceAdded
        await reactors.DiscoverAndRegister(EventStoreName.System, EventStoreNamespaceName.Default);

        var allEventStores = await storage.GetEventStores();
        foreach (var eventStore in allEventStores)
        {
            await InitializeEventStore(eventStore);
        }

        await authenticationService.EnsureDefaultAdminUser();
#if DEVELOPMENT
        await authenticationService.EnsureDefaultClientCredentials();
#endif
    }

    /// <summary>
    /// Initialize a single event store (namespaces, event types, managers, projections, reactors).
    /// Job rehydration is intentionally omitted — callers invoke it separately.
    /// </summary>
    /// <param name="eventStore">The <see cref="EventStoreName"/> to initialize.</param>
    /// <returns>Awaitable task.</returns>
    internal async Task InitializeEventStore(EventStoreName eventStore)
    {
        await eventTypes.DiscoverAndRegister(eventStore);

        var namespaces = grainFactory.GetGrain<INamespaces>(eventStore);
        await namespaces.EnsureDefault();

        var readModelsManager = grainFactory.GetGrain<IReadModelsManager>(eventStore);
        await readModelsManager.Ensure();

        var projectionsManager = grainFactory.GetGrain<IProjectionsManager>(eventStore);
        await projectionsManager.Ensure();

        var webhooksManager = grainFactory.GetGrain<IWebhooks>(eventStore);
        await webhooksManager.Ensure();

        var projectionDefinitions = await projectionsManager.GetProjectionDefinitions();
        await projectionsServiceClient.Register(eventStore, projectionDefinitions);

        var allNamespaces = await namespaces.GetAll();
        var reactorTasks = allNamespaces.Select(namespaceName => reactors.DiscoverAndRegister(eventStore, namespaceName));
        await Task.WhenAll(reactorTasks);
    }
}
