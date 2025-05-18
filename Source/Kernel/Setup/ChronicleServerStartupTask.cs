// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Grains.Jobs;
using Cratis.Chronicle.Grains.Namespaces;
using Cratis.Chronicle.Grains.Projections;
using Cratis.Chronicle.Storage;

namespace Orleans.Hosting;

/// <summary>
/// Represents a startup task for Chronicle.
/// </summary>
/// <param name="storage"><see cref="IStorage"/> for storing data.</param>
/// <param name="grainFactory"><see cref="IGrainFactory"/> for creating grains.</param>
internal class ChronicleServerStartupTask(
    IStorage storage,
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
        var eventStores = await storage.GetEventStores();
        foreach (var eventStore in eventStores)
        {
            var namespaces = grainFactory.GetGrain<INamespaces>(eventStore);
            await namespaces.EnsureDefault();

            var projectionsManager = grainFactory.GetGrain<IProjectionsManager>(eventStore);
            await projectionsManager.Ensure();

            var rehydrateAll = (await namespaces.GetAll()).Select(async namespaceName =>
            {
                var jobsManager = grainFactory.GetJobsManager(eventStore, namespaceName);
                await jobsManager.Rehydrate();
            });
            await Task.WhenAll(rehydrateAll);
        }
    }
}
