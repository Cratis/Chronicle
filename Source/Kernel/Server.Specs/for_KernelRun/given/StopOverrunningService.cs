// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Hosting;

namespace Cratis.Chronicle.Server.for_KernelRun.given;

/// <summary>
/// A hosted service that ignores the stop token and returns, without failing, after the shutdown timeout.
/// </summary>
public class StopOverrunningService : IHostedService
{
    /// <inheritdoc/>
    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <inheritdoc/>
    public Task StopAsync(CancellationToken cancellationToken) => Task.Delay(a_host._shutdownTimeout * 2, CancellationToken.None);
}
