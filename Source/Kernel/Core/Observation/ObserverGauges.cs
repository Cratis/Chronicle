// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Diagnostics.Metrics;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Diagnostics;
using Cratis.DependencyInjection;
using Cratis.Metrics;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Represents an implementation of <see cref="IObserverGauges"/> that publishes the observer gauges.
/// </summary>
/// <remarks>
/// The instruments are new and have no legacy name, so they are published under the convention names and attribute
/// keys whatever Telemetry:MetricNames selects. The per-observer gauges carry event store, namespace, event
/// sequence and observer id, never a partition, so there is one series per resident observer.
/// </remarks>
[Singleton]
public sealed class ObserverGauges : IObserverGauges
{
    readonly ConcurrentDictionary<ObserverKey, ObserverGaugeEntry> _entries = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="ObserverGauges"/> class.
    /// </summary>
    /// <param name="meter">The <see cref="IMeter{T}"/> owning the instruments.</param>
    public ObserverGauges([FromKeyedServices(WellKnown.MeterName)] IMeter<ObserverGauges> meter)
    {
        var actual = meter.ActualMeter;
        if (actual is null)
        {
            return;
        }

        actual.CreateObservableGauge(
            WellKnownMetricNames.ObserverFailedPartitions,
            ObserveFailedPartitions,
            WellKnownMetricNames.ObserverFailedPartitionsUnit,
            "Current number of failed partitions per resident observer, quarantined partitions included");
        actual.CreateObservableGauge(
            WellKnownMetricNames.ObserverQuarantinedPartitions,
            ObserveQuarantinedPartitions,
            WellKnownMetricNames.ObserverQuarantinedPartitionsUnit,
            "Current number of quarantined partitions per resident observer");
        actual.CreateObservableGauge(
            WellKnownMetricNames.ObserverRunningState,
            ObserveRunningState,
            WellKnownMetricNames.ObserverRunningStateUnit,
            "Current running state code per resident observer: 0 unknown, 1 active, 2 suspended, 3 replaying, 4 disconnected, 5 quarantined, 6 changing definition, 7 initializing");
        actual.CreateObservableGauge(
            WellKnownMetricNames.ObserverResident,
            ObserveResident,
            WellKnownMetricNames.ObserverResidentUnit,
            "Number of observers resident in this process per event store, namespace, observer type and running state");
    }

    /// <inheritdoc/>
    public ObserverGaugeEntry Register(ObserverKey observerKey, ObserverType observerType)
    {
        var entry = new ObserverGaugeEntry(observerKey, observerType, Release);
        _entries[observerKey] = entry;
        return entry;
    }

    /// <summary>
    /// Observes the failed partitions of every resident observer.
    /// </summary>
    /// <returns>One measurement per resident observer.</returns>
    internal IEnumerable<Measurement<int>> ObserveFailedPartitions() =>
        _entries.Values.Select(_ => new Measurement<int>(_.Snapshot.FailedPartitions, _.Tags)).ToArray();

    /// <summary>
    /// Observes the quarantined partitions of every resident observer.
    /// </summary>
    /// <returns>One measurement per resident observer.</returns>
    internal IEnumerable<Measurement<int>> ObserveQuarantinedPartitions() =>
        _entries.Values.Select(_ => new Measurement<int>(_.Snapshot.QuarantinedPartitions, _.Tags)).ToArray();

    /// <summary>
    /// Observes the running state code of every resident observer.
    /// </summary>
    /// <returns>One measurement per resident observer.</returns>
    internal IEnumerable<Measurement<int>> ObserveRunningState() =>
        _entries.Values.Select(_ => new Measurement<int>((int)_.Snapshot.RunningState, _.Tags)).ToArray();

    /// <summary>
    /// Observes how many observers are resident per event store, namespace, observer type and running state.
    /// </summary>
    /// <returns>One measurement per combination that has resident observers.</returns>
    internal IEnumerable<Measurement<int>> ObserveResident() =>
        _entries.Values
            .GroupBy(_ => (
                EventStore: _.ObserverKey.EventStore.Value,
                Namespace: _.ObserverKey.Namespace.Value,
                Type: ObserverTelemetryValues.For(_.ObserverType),
                RunningState: ObserverTelemetryValues.For(_.Snapshot.RunningState)))
            .Select(_ => new Measurement<int>(
                _.Count(),
                new KeyValuePair<string, object?>(WellKnownTelemetryAttributes.EventStoreName, _.Key.EventStore),
                new KeyValuePair<string, object?>(WellKnownTelemetryAttributes.EventStoreNamespace, _.Key.Namespace),
                new KeyValuePair<string, object?>(WellKnownTelemetryAttributes.ObserverType, _.Key.Type),
                new KeyValuePair<string, object?>(WellKnownTelemetryAttributes.ObserverRunningState, _.Key.RunningState)))
            .ToArray();

    void Release(ObserverGaugeEntry entry) =>
        _entries.TryRemove(new KeyValuePair<ObserverKey, ObserverGaugeEntry>(entry.ObserverKey, entry));
}
