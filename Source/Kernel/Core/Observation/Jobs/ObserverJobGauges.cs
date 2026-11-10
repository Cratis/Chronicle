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
/// </remarks>
[Singleton]
public sealed class ObserverJobGauges : IObserverJobGauges
{
    readonly ConcurrentDictionary<JobId, ObserverJobGaugeEntry> _entries = new();

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
    /// <returns>One measurement per combination that has running jobs.</returns>
    internal IEnumerable<Measurement<int>> ObserveRunning() =>
        _entries.Values
            .GroupBy(_ => (EventStore: _.ObserverKey.EventStore.Value, Namespace: _.ObserverKey.Namespace.Value, _.JobType))
            .Select(_ => new Measurement<int>(
                _.Count(),
                new KeyValuePair<string, object?>(WellKnownTelemetryAttributes.EventStoreName, _.Key.EventStore),
                new KeyValuePair<string, object?>(WellKnownTelemetryAttributes.EventStoreNamespace, _.Key.Namespace),
                new KeyValuePair<string, object?>(WellKnownTelemetryAttributes.JobType, For(_.Key.JobType))))
            .ToArray();

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
}
