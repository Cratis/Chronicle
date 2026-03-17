// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Jobs;
using Cratis.Chronicle.Namespaces;
using Cratis.Chronicle.Patching;
using Cratis.Chronicle.Storage;

namespace Orleans.Hosting;

/// <summary>
/// Represents a startup task for Chronicle.
/// </summary>
/// <param name="storage"><see cref="IStorage"/> for storing data.</param>
/// <param name="initializer"><see cref="Cratis.Chronicle.Setup.IChronicleInitializer"/> for bootstrapping Chronicle artifacts.</param>
/// <param name="grainFactory"><see cref="IGrainFactory"/> for creating grains.</param>
internal sealed class ChronicleServerStartupTask(
    IStorage storage,
    Cratis.Chronicle.Setup.IChronicleInitializer initializer,
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
        // Apply patches first before anything else starts
        var patchManager = grainFactory.GetGrain<IPatchManager>(0);
        await patchManager.ApplyPatches();

        await initializer.Initialize(cancellationToken);

        // Rehydrate jobs for all event stores / namespaces
        var allEventStores = await storage.GetEventStores();
        foreach (var eventStore in allEventStores)
        {
            var namespaces = grainFactory.GetGrain<INamespaces>(eventStore);
            var rehydrateAll = (await namespaces.GetAll()).Select(async namespaceName =>
            {
                var jobsManager = grainFactory.GetJobsManager(eventStore, namespaceName);
                await jobsManager.Rehydrate();
            });
            await Task.WhenAll(rehydrateAll);
        }
    }
}

