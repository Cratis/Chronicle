// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Security;

namespace Cratis.Chronicle.Server;

/// <summary>
/// Retains Orleans' configuration validation and start/stop behavior, deferring only its start phase until listeners bind.
/// </summary>
/// <param name="silo">The original Orleans hosted service.</param>
/// <param name="logger">The kernel logger.</param>
/// <param name="warmups">The framework key-ring warmups to run after the silo starts.</param>
internal sealed class DeferredSiloStart(IHostedService silo, ILogger<Kernel> logger, DataProtectionKeyRingWarmup[] warmups) : IHostedLifecycleService
{
    /// <inheritdoc/>
    public Task StartingAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <inheritdoc/>
    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <inheritdoc/>
    public async Task StartedAsync(CancellationToken cancellationToken)
    {
        logger.ListenerBoundSiloStarting();
        await silo.StartAsync(cancellationToken);
        foreach (var warmup in warmups) await warmup.Warmup(cancellationToken);
    }

    /// <inheritdoc/>
    public Task StoppingAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <inheritdoc/>
    public Task StopAsync(CancellationToken cancellationToken) => silo.StopAsync(cancellationToken);

    /// <inheritdoc/>
    public Task StoppedAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
