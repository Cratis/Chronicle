// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.Metrics;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Diagnostics;
using Cratis.Chronicle.Diagnostics.OpenTelemetry;
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
    readonly MetricNames _names;
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
        _names = options?.Value.Telemetry.MetricNames ?? MetricNames.Legacy;
        if (!_enabled)
        {
            return;
        }

        if (_names is MetricNames.Convention or MetricNames.Both)
        {
            meter.ActualMeter?.CreateObservableGauge(
                WellKnownMetricNames.AlertOpenIncidents,
                Observe,
                unit: WellKnownMetricNames.AlertOpenIncidentsUnit,
                description: "Number of open alert incidents per observer, condition and severity, sampled periodically from incident storage");
            meter.ActualMeter?.CreateObservableGauge(
                WellKnownMetricNames.AlertOpenIncidentsAvailable,
                ObserveAvailable,
                unit: WellKnownMetricNames.AlertOpenIncidentsAvailableUnit,
                description: "1 when this process owns a fresh open incident snapshot, 0 when it owns a missing or stale one; absent when this process is not the owner");
        }

        if (_names is MetricNames.Legacy or MetricNames.Both)
        {
            meter.ActualMeter?.CreateObservableGauge(
                LegacyTelemetryNames.OpenIncidents,
                ObserveLegacy,
                unit: null,
                description: "Number of open alert incidents per observer, condition and severity, sampled periodically from incident storage");
            meter.ActualMeter?.CreateObservableGauge(
                LegacyTelemetryNames.OpenIncidentsAvailable,
                ObserveAvailable,
                unit: null,
                description: "1 when this process owns a fresh open incident snapshot, 0 when it owns a missing or stale one; absent when this process is not the owner");
        }
    }

    /// <inheritdoc/>
    void IAlertIncidentsGauge.Activate(object owner) => _state = new(owner, null);

    /// <inheritdoc/>
    void IAlertIncidentsGauge.Publish(object owner, AlertIncidentsGaugeSnapshot snapshot) =>
        _state = new(owner, snapshot, _names is MetricNames.Legacy or MetricNames.Both ? snapshot.WithLegacyTags() : []);

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
    /// Gets the current measurements with the legacy attribute keys; empty when there is no fresh snapshot.
    /// </summary>
    /// <returns>The measurements.</returns>
    internal IEnumerable<Measurement<long>> ObserveLegacy()
    {
        var state = _state;
        return !_enabled || Fresh(state) is null ? [] : state.LegacyMeasurements ?? [];
    }

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

    sealed record State(object? Owner, AlertIncidentsGaugeSnapshot? Snapshot, IReadOnlyList<Measurement<long>>? LegacyMeasurements = null)
    {
        internal static readonly State None = new(null, null);
    }
}
