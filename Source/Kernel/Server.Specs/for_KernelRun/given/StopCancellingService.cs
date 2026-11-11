// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Hosting;

namespace Cratis.Chronicle.Server.for_KernelRun.given;

/// <summary>
/// A hosted service that reports its own cancellation while stopping, long before the shutdown timeout.
/// </summary>
public class StopCancellingService : IHostedService
{
    /// <inheritdoc/>
    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <inheritdoc/>
    public Task StopAsync(CancellationToken cancellationToken) => throw new OperationCanceledException("The service gave up on its own");
}
