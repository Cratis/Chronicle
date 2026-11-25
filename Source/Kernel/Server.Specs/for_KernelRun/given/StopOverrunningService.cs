// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Hosting;

namespace Cratis.Chronicle.Server.for_KernelRun.given;

/// <summary>
/// A hosted service that ignores the stop token and returns only when the spec releases it.
/// </summary>
public class StopOverrunningService : IHostedService
{
    readonly TaskCompletionSource _stopping = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _completion = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>
    /// Gets the signal that the service has entered its stop operation.
    /// </summary>
    public Task Stopping => _stopping.Task;

    /// <summary>
    /// Allows the stop operation to complete after the spec has expired the deadline.
    /// </summary>
    public void Complete() => _completion.SetResult();

    /// <inheritdoc/>
    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <inheritdoc/>
    public Task StopAsync(CancellationToken cancellationToken)
    {
        _stopping.SetResult();
        return _completion.Task;
    }
}
