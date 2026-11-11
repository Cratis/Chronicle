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
/// <para>
/// The instruments are new and have no legacy name, so they are published under the convention names and attribute
/// keys whatever Telemetry:MetricNames selects.
/// </para>
/// <para>
/// The per-observer gauges carry event store, namespace, event sequence and observer id, never a partition, and
/// report an observer only while it needs attention: failed or quarantined partitions above zero, or a running state in
/// <see cref="AttentionStates"/>. After it recovers, or is released while abnormal, its series reports zero for
/// <see cref="ZeroTail"/>, and at least once whatever the collection interval, and then stops. A healthy observer costs no series. Under cumulative temporality the SDK keeps
/// a series' slot until the process stops, so the series count per instrument is bounded by the observers that were
/// ever abnormal while the process ran, not by the resident observers.
/// </para>
/// <para>
/// <see cref="WellKnownMetricNames.ObserverResident"/> counts every resident observer per event store, namespace, type
/// and running state, and keeps reporting zero for a combination it has seen once it empties.
/// </para>
/// </remarks>
[Singleton]
public sealed class ObserverGauges : IObserverGauges
{
    /// <summary>
    /// The running states reported per observer.
    /// </summary>
    /// <remarks>
    /// Disconnected and replaying observers are expected in normal operation, so they are counted only in the
    /// resident gauge, which stays bounded whatever the number of observers.
    /// </remarks>
    public static readonly IReadOnlyList<ObserverRunningState> AttentionStates = [ObserverRunningState.Quarantined, ObserverRunningState.Suspended];

    /// <summary>
    /// How long a per-observer series keeps reporting zero after it was last above zero.
    /// </summary>
    /// <remarks>
    /// Long enough for several collections at the default 60 second export interval.
    /// </remarks>
    public static readonly TimeSpan ZeroTail = TimeSpan.FromMinutes(3);

    readonly ConcurrentDictionary<ObserverKey, ObserverGaugeEntry> _entries = new();
    readonly ConcurrentDictionary<ResidentGroup, byte> _residentGroups = new();
    readonly AbnormalGaugeSeries<ObserverKey> _failed;
    readonly AbnormalGaugeSeries<ObserverKey> _quarantined;
    readonly AbnormalGaugeSeries<(ObserverKey ObserverKey, ObserverRunningState State)> _runningState;

    /// <summary>
    /// Initializes a new instance of the <see cref="ObserverGauges"/> class.
    /// </summary>
    /// <param name="meter">The <see cref="IMeter{T}"/> owning the instruments.</param>
    /// <param name="timeProvider">The <see cref="TimeProvider"/> measuring the zero tail; the system clock when not given.</param>
    public ObserverGauges([FromKeyedServices(WellKnown.MeterName)] IMeter<ObserverGauges> meter, TimeProvider? timeProvider = null)
    {
        var clock = timeProvider ?? TimeProvider.System;
        _failed = new(clock, ZeroTail);
        _quarantined = new(clock, ZeroTail);
        _runningState = new(clock, ZeroTail);

        var actual = meter.ActualMeter;
        if (actual is null)
        {
            return;
        }

        actual.CreateObservableGauge(
            WellKnownMetricNames.ObserverFailedPartitions,
            ObserveFailedPartitions,
            WellKnownMetricNames.ObserverFailedPartitionsUnit,
            "Current number of failed partitions, quarantined ones included, of each resident observer that has any");
        actual.CreateObservableGauge(
            WellKnownMetricNames.ObserverQuarantinedPartitions,
            ObserveQuarantinedPartitions,
            WellKnownMetricNames.ObserverQuarantinedPartitionsUnit,
            "Current number of quarantined partitions of each resident observer that has any");
        actual.CreateObservableGauge(
            WellKnownMetricNames.ObserverRunningState,
            ObserveRunningState,
            WellKnownMetricNames.ObserverRunningStateUnit,
            "1 while a resident observer is in the running state named by its state attribute, reported for quarantined and suspended observers only");
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
    /// Observes the failed partitions of the resident observers that have any.
    /// </summary>
    /// <returns>One measurement per observer with failed partitions, and zeros for recently recovered ones.</returns>
    internal IEnumerable<Measurement<int>> ObserveFailedPartitions() =>
        _failed.Observe(_entries.Values.Select(_ => (_.ObserverKey, _.Snapshot.FailedPartitions, _.Tags)));

    /// <summary>
    /// Observes the quarantined partitions of the resident observers that have any.
    /// </summary>
    /// <returns>One measurement per observer with quarantined partitions, and zeros for recently recovered ones.</returns>
    internal IEnumerable<Measurement<int>> ObserveQuarantinedPartitions() =>
        _quarantined.Observe(_entries.Values.Select(_ => (_.ObserverKey, _.Snapshot.QuarantinedPartitions, _.Tags)));

    /// <summary>
    /// Observes the resident observers in a running state that needs attention.
    /// </summary>
    /// <returns>A 1 per observer and attention state it is in, and zeros for states it recently left.</returns>
    internal IEnumerable<Measurement<int>> ObserveRunningState() =>
        _runningState.Observe(_entries.Values
            .Select(_ => (Entry: _, _.Snapshot.RunningState))
            .Where(_ => AttentionStates.Contains(_.RunningState))
            .Select(_ => ((_.Entry.ObserverKey, _.RunningState), 1, _.Entry.TagsWithState(_.RunningState))));

    /// <summary>
    /// Observes how many observers are resident per event store, namespace, observer type and running state.
    /// </summary>
    /// <returns>One measurement per combination seen in this process, zero when it has no resident observers.</returns>
    internal IEnumerable<Measurement<int>> ObserveResident()
    {
        var counts = _entries.Values
            .GroupBy(_ => new ResidentGroup(
                _.ObserverKey.EventStore.Value,
                _.ObserverKey.Namespace.Value,
                ObserverTelemetryValues.For(_.ObserverType),
                ObserverTelemetryValues.For(_.Snapshot.RunningState)))
            .ToDictionary(_ => _.Key, _ => _.Count());

        foreach (var group in counts.Keys)
        {
            _residentGroups.TryAdd(group, 0);
        }

        return _residentGroups.Keys
            .Select(group => new Measurement<int>(
                counts.GetValueOrDefault(group),
                new KeyValuePair<string, object?>(WellKnownTelemetryAttributes.EventStoreName, group.EventStore),
                new KeyValuePair<string, object?>(WellKnownTelemetryAttributes.EventStoreNamespace, group.Namespace),
                new KeyValuePair<string, object?>(WellKnownTelemetryAttributes.ObserverType, group.Type),
                new KeyValuePair<string, object?>(WellKnownTelemetryAttributes.ObserverState, group.State)))
            .ToArray();
    }

    void Release(ObserverGaugeEntry entry) =>
        _entries.TryRemove(new KeyValuePair<ObserverKey, ObserverGaugeEntry>(entry.ObserverKey, entry));

    sealed record ResidentGroup(string EventStore, string Namespace, string Type, string State);
}
