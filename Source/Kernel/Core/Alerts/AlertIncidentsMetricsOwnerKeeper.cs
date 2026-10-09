// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Alerts;

/// <summary>
/// Pings the open incident gauge owner from every silo so it is re-activated after the silo hosting it dies.
/// </summary>
/// <param name="grainFactory">The grain factory.</param>
/// <param name="logger">The logger.</param>
/// <param name="timeProvider">Optional time provider.</param>
/// <param name="options">The startup configuration.</param>
public sealed class AlertIncidentsMetricsOwnerKeeper(
    IGrainFactory grainFactory,
    ILogger<AlertIncidentsMetricsOwnerKeeper> logger,
    TimeProvider? timeProvider = null,
    IOptions<ChronicleOptions>? options = null) : ILifecycleParticipant<ISiloLifecycle>, IDisposable
{
    readonly bool _enabled = options?.Value.Alerts.IncidentMetricsEnabled ?? true;
    CancellationTokenSource? _cancellation;
    Task _loop = Task.CompletedTask;

    /// <inheritdoc/>
    public void Dispose() => _cancellation?.Dispose();

    /// <inheritdoc/>
    public void Participate(ISiloLifecycle lifecycle)
    {
        if (_enabled)
        {
            lifecycle.Subscribe(nameof(AlertIncidentsMetricsOwnerKeeper), ServiceLifecycleStage.Active, Start, Stop);
        }
    }

    /// <summary>
    /// Pings the owner grain; failures are logged and swallowed.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    internal async Task Ping()
    {
        if (!_enabled)
        {
            return;
        }

        try
        {
            await grainFactory.GetGrain<IAlertIncidentsMetricsOwner>(AlertIncidentsMetricsOwner.Key).Ensure();
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.PingFailed(exception);
        }
    }

    Task Start(CancellationToken cancellationToken)
    {
        _cancellation = new();
        _loop = Task.Run(() => Loop(_cancellation.Token), CancellationToken.None);
        return Task.CompletedTask;
    }

    async Task Stop(CancellationToken cancellationToken)
    {
        if (_cancellation is null)
        {
            return;
        }

        await _cancellation.CancelAsync();
        try
        {
            await _loop.WaitAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
        }
    }

    async Task Loop(CancellationToken cancellationToken)
    {
        try
        {
            await Ping();
            using var timer = new PeriodicTimer(AlertIncidentsGaugeTiming.PingPeriod, timeProvider ?? TimeProvider.System);
            while (await timer.WaitForNextTickAsync(cancellationToken))
            {
                await Ping();
            }
        }
        catch (OperationCanceledException)
        {
        }
    }
}
