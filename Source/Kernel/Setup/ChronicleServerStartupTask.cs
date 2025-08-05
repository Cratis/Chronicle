// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Grains.EventTypes.Kernel;
using Cratis.Chronicle.Grains.Jobs;
using Cratis.Chronicle.Grains.Observation.Reactors.Kernel;
using Cratis.Chronicle.Grains.Projections;
using Cratis.Chronicle.Storage;
using INamespaces = Cratis.Chronicle.Grains.Namespaces.INamespaces;

namespace Orleans.Hosting;

/// <summary>
/// Represents a startup task for Chronicle.
/// </summary>
/// <param name="storage"><see cref="IStorage"/> for storing data.</param>
/// <param name="eventTypes"><see cref="IEventTypes"/> for managing kernel event types.</param>
/// <param name="reactors"><see cref="IReactors"/> for managing kernel reactors.</param>
/// <param name="eventStores"><see cref="IEventStores"/> for managing event stores.</param>
/// <param name="grainFactory"><see cref="IGrainFactory"/> for creating grains.</param>
internal sealed class ChronicleServerStartupTask(
    IStorage storage,
    IEventTypes eventTypes,
    IReactors reactors,
    IEventStores eventStores,
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
        await eventStores.Ensure(new() { Name = EventStoreName.System });
        await eventTypes.DiscoverAndRegister();

        var allEventStores = await storage.GetEventStores();
        foreach (var eventStore in allEventStores)
        {
            var namespaces = grainFactory.GetGrain<INamespaces>(eventStore);
            await namespaces.EnsureDefault();

            var projectionsManager = grainFactory.GetGrain<IProjectionsManager>(eventStore);
            await projectionsManager.Ensure();

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
