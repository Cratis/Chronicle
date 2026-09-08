// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias KernelCore;
extern alias KernelConcepts;

using Microsoft.Extensions.DependencyInjection;
using INamespaces = KernelCore::Cratis.Chronicle.Namespaces.INamespaces;
using IReactors = KernelCore::Cratis.Chronicle.Observation.Reactors.Kernel.IReactors;
using KernelEventStoreName = KernelConcepts::Cratis.Chronicle.Concepts.EventStoreName;
using KernelEventStoreNamespaceName = KernelConcepts::Cratis.Chronicle.Concepts.EventStoreNamespaceName;

namespace Cratis.Chronicle.Benchmarks.Clustering;

/// <summary>
/// Performs the kernel bootstrap that <c>ChronicleServerStartupTask</c> normally does at silo startup.
/// </summary>
/// <remarks>
/// The startup task activates grains while the silo is still starting, which with role based placement
/// either deadlocks or fails placement in a test cluster. It is removed from the silo's services and the
/// equivalent work is driven from here once every silo is deployed and membership has stabilized.
/// </remarks>
public static class KernelBootstrap
{
    /// <summary>
    /// Creates the system and user namespaces and registers the system reactors.
    /// </summary>
    /// <param name="services">The <see cref="IServiceProvider"/> of a deployed silo.</param>
    /// <param name="eventStore">The name of the event store the benchmarks use.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public static async Task Run(IServiceProvider services, string eventStore)
    {
        var grainFactory = services.GetRequiredService<IGrainFactory>();
        var kernelReactors = services.GetRequiredService<IReactors>();

        await grainFactory.GetGrain<INamespaces>((string)KernelEventStoreName.System).EnsureDefault();
        await kernelReactors.DiscoverAndRegister(KernelEventStoreName.System, KernelEventStoreNamespaceName.Default);

        await grainFactory.GetGrain<INamespaces>(eventStore).EnsureDefault();
        await kernelReactors.DiscoverAndRegister(eventStore, KernelEventStoreNamespaceName.Default);
    }

    /// <summary>
    /// Removes the <c>ChronicleServerStartupTask</c> registration from a silo's services.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to adjust.</param>
    public static void RemoveServerStartupTask(IServiceCollection services)
    {
        var startupTaskType = typeof(INamespaces).Assembly
            .GetType("Orleans.Hosting.ChronicleServerStartupTask");
        if (startupTaskType is null)
        {
            return;
        }

        foreach (var descriptor in services.Where(descriptor => descriptor.ImplementationType == startupTaskType).ToList())
        {
            services.Remove(descriptor);
        }
    }
}
