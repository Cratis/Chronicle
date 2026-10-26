// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Hosting;

namespace Cratis.Chronicle.Diagnostics.OpenTelemetry;

/// <summary>
/// Keeps the legacy metric names published for as long as the host runs.
/// </summary>
internal sealed class LegacyMetricNameBridgeHostedService : IHostedService, IDisposable
{
    IDisposable? _lease;

    /// <inheritdoc/>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _lease ??= SharedLegacyMetricNameBridge.Acquire();
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task StopAsync(CancellationToken cancellationToken)
    {
        Dispose();
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public void Dispose() => Interlocked.Exchange(ref _lease, null)?.Dispose();
}
