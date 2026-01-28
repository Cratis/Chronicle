// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Grains.EventTypes.Kernel;
using Cratis.Chronicle.Grains.Jobs;
using Cratis.Chronicle.Grains.Namespaces;
using Cratis.Chronicle.Grains.Observation.Reactors.Kernel;
using Cratis.Chronicle.Grains.Projections;
using Cratis.Chronicle.Grains.ReadModels;
using Cratis.Chronicle.Storage;

namespace Orleans.Hosting;

/// <summary>
/// Represents a startup task for Chronicle.
/// </summary>
/// <param name="storage"><see cref="IStorage"/> for storing data.</param>
/// <param name="eventTypes"><see cref="IEventTypes"/> for managing kernel event types.</param>
/// <param name="reactors"><see cref="IReactors"/> for managing kernel reactors.</param>
/// <param name="projectionsServiceClient"><see cref="IProjectionsServiceClient"/> for registering projections with local silos.</param>
/// <param name="grainFactory"><see cref="IGrainFactory"/> for creating grains.</param>
internal sealed class ChronicleServerStartupTask(
    IStorage storage,
    IEventTypes eventTypes,
    IReactors reactors,
    IProjectionsServiceClient projectionsServiceClient,
    IGrainFactory grainFactory) : ILifecycleParticipant<ISiloLifecycle>
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
        await grainFactory.GetGrain<INamespaces>(EventStoreName.System).EnsureDefault();

        await eventTypes.DiscoverAndRegister();

        var allEventStores = await storage.GetEventStores();
        foreach (var eventStore in allEventStores)
        {
            var namespaces = grainFactory.GetGrain<INamespaces>(eventStore);
            await namespaces.EnsureDefault();

            var readModelsManager = grainFactory.GetGrain<IReadModelsManager>(eventStore);
            await readModelsManager.Ensure();

            var projectionsManager = grainFactory.GetGrain<IProjectionsManager>(eventStore);
            await projectionsManager.Ensure();

            var projectionDefinitions = await projectionsManager.GetProjectionDefinitions();
            await projectionsServiceClient.Register(eventStore, projectionDefinitions);

            var rehydrateAll = (await namespaces.GetAll()).Select(async namespaceName =>
            {
                await reactors.DiscoverAndRegister(eventStore, namespaceName);

                var jobsManager = grainFactory.GetJobsManager(eventStore, namespaceName);
                await jobsManager.Rehydrate();
            });
            await Task.WhenAll(rehydrateAll);
        }
    }
}
