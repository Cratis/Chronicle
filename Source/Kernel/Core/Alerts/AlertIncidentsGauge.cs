// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.Metrics;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Configuration;
using Cratis.DependencyInjection;
using Cratis.Metrics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Alerts;

/// <summary>
/// Represents an implementation of <see cref="IAlertIncidentsGauge"/> exposing observable gauges that read a snapshot without I/O.
/// </summary>
[Singleton]
public sealed class AlertIncidentsGauge : IAlertIncidentsGauge
{
    readonly TimeProvider _timeProvider;
    readonly bool _enabled;
    volatile State _state = State.None;

    /// <summary>
    /// Initializes a new instance of the <see cref="AlertIncidentsGauge"/> class.
    /// </summary>
    /// <param name="meter">The meter.</param>
    /// <param name="timeProvider">Optional time provider.</param>
    /// <param name="options">The startup configuration.</param>
    public AlertIncidentsGauge([FromKeyedServices(WellKnown.MeterName)] IMeter<AlertIncidentsGauge> meter, TimeProvider? timeProvider = null, IOptions<ChronicleOptions>? options = null)
    {
        _timeProvider = timeProvider ?? TimeProvider.System;
        _enabled = options?.Value.Alerts.IncidentMetricsEnabled ?? true;
        if (!_enabled)
        {
            return;
        }

        // Hand-written until Cratis/Fundamentals#1138 is consumed: the metrics generator passes description as unit.
        meter.ActualMeter?.CreateObservableGauge(
            "chronicle-alerts-open-incidents",
            Observe,
            unit: null,
            description: "Number of open alert incidents per observer, condition and severity, sampled periodically from incident storage");
        meter.ActualMeter?.CreateObservableGauge(
            "chronicle-alerts-open-incidents-available",
            ObserveAvailable,
            unit: null,
            description: "1 when this process owns a fresh open incident snapshot, 0 when it owns a missing or stale one; absent when this process is not the owner");
    }

    /// <inheritdoc/>
    void IAlertIncidentsGauge.Activate(object owner) => _state = new(owner, null);

    /// <inheritdoc/>
    void IAlertIncidentsGauge.Publish(object owner, AlertIncidentsGaugeSnapshot snapshot) =>
        _state = new(owner, snapshot);

    /// <inheritdoc/>
    void IAlertIncidentsGauge.Clear(object owner)
    {
        var current = _state;
        if (ReferenceEquals(current.Owner, owner))
        {
            Interlocked.CompareExchange(ref _state, State.None, current);
        }
    }

    /// <summary>
    /// Gets the current measurements; empty when there is no fresh snapshot.
    /// </summary>
    /// <returns>The measurements.</returns>
    internal IEnumerable<Measurement<long>> Observe() => _enabled ? Fresh(_state)?.Measurements ?? [] : [];

    /// <summary>
    /// Gets the availability measurement; empty when this process has no owner.
    /// </summary>
    /// <returns>The measurement.</returns>
    internal IEnumerable<Measurement<int>> ObserveAvailable()
    {
        var state = _state;
        return !_enabled || state.Owner is null ? [] : [new Measurement<int>(Fresh(state) is null ? 0 : 1)];
    }

    AlertIncidentsGaugeSnapshot? Fresh(State state) =>
        state.Snapshot is not null && _timeProvider.GetUtcNow() - state.Snapshot.TakenAt <= AlertIncidentsGaugeTiming.StalenessThreshold ? state.Snapshot : null;

    sealed record State(object? Owner, AlertIncidentsGaugeSnapshot? Snapshot)
    {
        internal static readonly State None = new(null, null);
    }
}
