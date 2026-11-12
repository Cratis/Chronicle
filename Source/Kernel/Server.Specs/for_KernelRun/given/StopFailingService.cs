// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Hosting;

namespace Cratis.Chronicle.Server.for_KernelRun.given;

/// <summary>
/// A hosted service that fails while the host is stopping.
/// </summary>
public class StopFailingService : IHostedService
{
    /// <inheritdoc/>
    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <inheritdoc/>
    public Task StopAsync(CancellationToken cancellationToken) => throw new InvalidOperationException("The service failed to stop");
}
