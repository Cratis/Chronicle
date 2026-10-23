// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Server;

/// <summary>
/// Retains Orleans' configuration validation and start/stop behavior, deferring only its start phase until listeners bind.
/// </summary>
/// <param name="silo">The original Orleans hosted service.</param>
/// <param name="logger">The kernel logger.</param>
internal sealed class DeferredSiloStart(IHostedService silo, ILogger<Kernel> logger) : IHostedLifecycleService
{
    /// <inheritdoc/>
    public Task StartingAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <inheritdoc/>
    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <inheritdoc/>
    public Task StartedAsync(CancellationToken cancellationToken)
    {
        logger.ListenerBoundSiloStarting();
        return silo.StartAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public Task StoppingAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <inheritdoc/>
    public Task StopAsync(CancellationToken cancellationToken) => silo.StopAsync(cancellationToken);

    /// <inheritdoc/>
    public Task StoppedAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
