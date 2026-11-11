// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Hosting;

namespace Cratis.Chronicle.Server.for_KernelRun.given;

/// <summary>
/// A hosted service that is still starting when a stop is requested, and gives up starting when it is.
/// </summary>
/// <param name="stopRequest">The stop request to make while starting.</param>
public class StopRequestingStartService(CancellationTokenSource stopRequest) : IHostedService
{
    /// <inheritdoc/>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await stopRequest.CancelAsync();
        await Task.Delay(Timeout.Infinite, cancellationToken);
    }

    /// <inheritdoc/>
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
