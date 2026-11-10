// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Diagnostics.Metrics;
using Cratis.Choreography.Jobs;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Diagnostics;
using Cratis.DependencyInjection;
using Cratis.Metrics;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Observation.Jobs;

/// <summary>
/// Represents an implementation of <see cref="IObserverJobGauges"/> that publishes the observer job gauges.
/// </summary>
/// <remarks>
/// The instruments are new and have no legacy name, so they are published under the convention names and attribute
/// keys whatever Telemetry:MetricNames selects. No job id is ever an attribute: running jobs are counted per
/// event store, namespace and job type, and progress is summed per observer and job type.
/// The running count keeps reporting zero for a combination that had a job once it has none. Progress is reported only
/// while a job runs, but under cumulative temporality the SDK keeps a series' slot until the process stops, so the
/// progress series per instrument are bounded by the observers that ran a catch-up or replay while the process ran.
/// </remarks>
[Singleton]
public sealed class ObserverJobGauges : IObserverJobGauges
{
    readonly ConcurrentDictionary<JobId, ObserverJobGaugeEntry> _entries = new();
    readonly ConcurrentDictionary<RunningGroup, byte> _runningGroups = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="ObserverJobGauges"/> class.
    /// </summary>
    /// <param name="meter">The <see cref="IMeter{T}"/> owning the instruments.</param>
    public ObserverJobGauges([FromKeyedServices(WellKnown.MeterName)] IMeter<ObserverJobGauges> meter)
    {
        var actual = meter.ActualMeter;
        if (actual is null)
        {
            return;
        }

        actual.CreateObservableGauge(
            WellKnownMetricNames.JobRunning,
            ObserveRunning,
            WellKnownMetricNames.JobRunningUnit,
            "Number of observer jobs running in this process per event store, namespace and job type");
        actual.CreateObservableGauge(
            WellKnownMetricNames.ObserverJobHandledSteps,
            ObserveHandledSteps,
            WellKnownMetricNames.ObserverJobHandledStepsUnit,
            "Number of completed steps of the running catch-up or replay jobs of an observer");
        actual.CreateObservableGauge(
            WellKnownMetricNames.ObserverJobTotalSteps,
            ObserveTotalSteps,
            WellKnownMetricNames.ObserverJobTotalStepsUnit,
            "Number of steps of the running catch-up or replay jobs of an observer");
    }

    /// <inheritdoc/>
    public ObserverJobGaugeEntry Register(JobId jobId, ObserverKey observerKey, ObserverJobGaugeType jobType)
    {
        var entry = new ObserverJobGaugeEntry(jobId, observerKey, jobType, Release);
        _runningGroups.TryAdd(new(observerKey.EventStore.Value, observerKey.Namespace.Value, jobType), 0);
        _entries[jobId] = entry;
        return entry;
    }

    /// <summary>
    /// Gets the attribute value of an <see cref="ObserverJobGaugeType"/>.
    /// </summary>
    /// <param name="jobType">The job type.</param>
    /// <returns>The lowercase value.</returns>
    internal static string For(ObserverJobGaugeType jobType) => jobType switch
    {
        ObserverJobGaugeType.Replay => "replay",
        _ => "catch_up"
    };

    /// <summary>
    /// Observes how many observer jobs run per event store, namespace and job type.
    /// </summary>
    /// <returns>One measurement per combination that had a running job in this process, zero when it has none now.</returns>
    internal IEnumerable<Measurement<int>> ObserveRunning()
    {
        var counts = _entries.Values
            .GroupBy(_ => new RunningGroup(_.ObserverKey.EventStore.Value, _.ObserverKey.Namespace.Value, _.JobType))
            .ToDictionary(_ => _.Key, _ => _.Count());

        return _runningGroups.Keys
            .Select(group => new Measurement<int>(
                counts.GetValueOrDefault(group),
                new KeyValuePair<string, object?>(WellKnownTelemetryAttributes.EventStoreName, group.EventStore),
                new KeyValuePair<string, object?>(WellKnownTelemetryAttributes.EventStoreNamespace, group.Namespace),
                new KeyValuePair<string, object?>(WellKnownTelemetryAttributes.JobType, For(group.JobType))))
            .ToArray();
    }

    /// <summary>
    /// Observes the completed steps of the running jobs, summed per observer and job type.
    /// </summary>
    /// <returns>One measurement per observer and job type with running jobs.</returns>
    internal IEnumerable<Measurement<long>> ObserveHandledSteps() => ObservePerObserver(_ => _.HandledSteps);

    /// <summary>
    /// Observes the steps of the running jobs, summed per observer and job type.
    /// </summary>
    /// <returns>One measurement per observer and job type with running jobs.</returns>
    internal IEnumerable<Measurement<long>> ObserveTotalSteps() => ObservePerObserver(_ => _.TotalSteps);

    Measurement<long>[] ObservePerObserver(Func<ObserverJobGaugeProgress, int> select) =>
        _entries.Values
            .GroupBy(_ => (_.ObserverKey, _.JobType))
            .Select(_ => new Measurement<long>(
                _.Sum(entry => (long)select(entry.Progress)),
                new KeyValuePair<string, object?>(WellKnownTelemetryAttributes.EventStoreName, _.Key.ObserverKey.EventStore.Value),
                new KeyValuePair<string, object?>(WellKnownTelemetryAttributes.EventStoreNamespace, _.Key.ObserverKey.Namespace.Value),
                new KeyValuePair<string, object?>(WellKnownTelemetryAttributes.EventSequenceId, _.Key.ObserverKey.EventSequenceId.Value),
                new KeyValuePair<string, object?>(WellKnownTelemetryAttributes.ObserverId, _.Key.ObserverKey.ObserverId.Value),
                new KeyValuePair<string, object?>(WellKnownTelemetryAttributes.JobType, For(_.Key.JobType))))
            .ToArray();

    void Release(ObserverJobGaugeEntry entry) =>
        _entries.TryRemove(new KeyValuePair<JobId, ObserverJobGaugeEntry>(entry.JobId, entry));

    sealed record RunningGroup(string EventStore, string Namespace, ObserverJobGaugeType JobType);
}
