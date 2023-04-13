// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans;
using Orleans.Runtime;

namespace Aksio.Cratis.Kernel.Grains.Silos;

/// <summary>
/// Represents a startup task for when the Silo is active for performing dead silos scavenging.
/// </summary>
public class DeadSilosScavengerStartupTask : ILifecycleParticipant<ISiloLifecycle>
{
    readonly IGrainFactory _grainFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeadSilosScavengerStartupTask"/> class.
    /// </summary>
    /// <param name="grainFactory">The <see cref="IGrainFactory"/> for working with grains.</param>
    public DeadSilosScavengerStartupTask(IGrainFactory grainFactory)
    {
        _grainFactory = grainFactory;
    }

    /// <inheritdoc/>
    public void Participate(ISiloLifecycle lifecycle)
    {
        lifecycle.Subscribe(
            nameof(DeadSilosScavengerStartupTask),
            ServiceLifecycleStage.Active,
            CleanUp,
            ctx => Task.CompletedTask);
    }

    Task CleanUp(CancellationToken cancellationToken) => _grainFactory.GetGrain<IDeadSilosScavenger>(0).CleanUp();
}
