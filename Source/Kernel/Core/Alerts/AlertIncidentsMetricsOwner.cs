// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Alerts;

/// <summary>
/// Represents the single grain that periodically aggregates open incidents and publishes them to the gauge holder.
/// </summary>
/// <param name="storage">The storage registry.</param>
/// <param name="readiness">The incident readiness sampler.</param>
/// <param name="gauge">The process-level gauge holder.</param>
/// <param name="logger">The logger.</param>
/// <param name="timeProvider">Optional time provider.</param>
/// <param name="options">The startup configuration.</param>
[KeepAlive]
public sealed class AlertIncidentsMetricsOwner(
    IStorage storage,
    IAlertIncidentsReadiness readiness,
    IAlertIncidentsGauge gauge,
    ILogger<AlertIncidentsMetricsOwner> logger,
    TimeProvider? timeProvider = null,
    IOptions<ChronicleOptions>? options = null) : Grain, IAlertIncidentsMetricsOwner
{
    /// <summary>
    /// The fixed grain key.
    /// </summary>
    public const long Key = 0;

    /// <summary>
    /// The options of the periodic refresh timer.
    /// </summary>
    internal static readonly GrainTimerCreationOptions TimerOptions = new()
    {
        DueTime = TimeSpan.Zero,
        Period = AlertIncidentsGaugeTiming.RefreshPeriod,
        KeepAlive = true,
        Interleave = false
    };

    readonly bool _enabled = options?.Value.Alerts.IncidentMetricsEnabled ?? true;
    readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;
    AlertIncidentGaugeBuckets _buckets = AlertIncidentGaugeBuckets.Empty;
    IGrainTimer? _timer;

    /// <inheritdoc/>
    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        if (!_enabled)
        {
            return Task.CompletedTask;
        }

        gauge.Activate(this);
        _timer = this.RegisterGrainTimer(_ => RefreshAsync(), TimerOptions);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public override Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        _timer?.Dispose();
        _timer = null;
        gauge.Clear(this);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task Ensure() => Task.CompletedTask;

    /// <summary>
    /// Refreshes the snapshot. Only a Ready, successful refresh replaces it; anything else keeps the previous snapshot to age out.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    internal async Task RefreshAsync()
    {
        if (!_enabled)
        {
            return;
        }

        try
        {
            var state = await readiness.Get();
            if (state != AlertIncidentsReadinessState.Ready)
            {
                logger.RefreshSkipped(state);
                return;
            }

            var counts = await storage.GetEventStore(EventStoreName.System).GetNamespace(EventStoreNamespaceName.Default).AlertIncidents.GetOpenCountsByObserver();
            var now = _timeProvider.GetUtcNow();
            _buckets = _buckets.Next(counts, now, AlertIncidentsGaugeTiming.ZeroRetention);
            gauge.Publish(this, AlertIncidentsGaugeSnapshot.From(now, _buckets.Values));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.RefreshFailed(exception);
        }
    }
}
