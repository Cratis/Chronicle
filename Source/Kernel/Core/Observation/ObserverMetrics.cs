// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Diagnostics.Metrics;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Metrics;

namespace Cratis.Chronicle.Observation;

#pragma warning disable SA1600 // Elements should be documented
#pragma warning disable MA0048 // File name must match type name
#pragma warning disable SA1402 // File may only contain a single type

internal static partial class ObserverMetrics
{
    const string PartitionFailedName = "chronicle-observer-partitions-failed";
    const string PartitionFailedDescription = "Number of failed handling attempts for partitions per observer in a given event store and namespace. Every failure is counted, including failures of retries, so the same partition counts once per failed attempt";
    const string PartitionRetryAttemptName = "chronicle-observer-partition-retry-attempts";
    const string PartitionRetryAttemptDescription = "Number of times a failed partition was evaluated for retry per observer. Incremented on every partition failure, whether or not a retry ends up being scheduled, so it does not count retries that actually ran";
    const string PartitionQuarantineClearedName = "chronicle-observer-partition-quarantines-cleared";
    const string PartitionQuarantineClearedDescription = "Number of times an operator cleared the quarantine of a partition per observer, which also resets the retry budget of the partition";
    const string PartitionQuarantinedName = "chronicle-observer-partitions-quarantined";
    const string PartitionQuarantinedDescription = "Number of partitions per observer that ran out of retry attempts and were quarantined. Does not count the observer itself being quarantined, see chronicle-observer-quarantined";
    const string ObserverQuarantinedName = "chronicle-observer-quarantined";
    const string ObserverQuarantinedDescription = "Number of times an observer was quarantined";

    static readonly Lock _failureCountersLock = new();
    static FailureCounters? _failureCounters;

    [Counter<int>("chronicle-observer-successful-observations", "Number of successful observations per observer in a given event store and namespace")]
    internal static partial void SuccessfulObservation(this IMeterScope<Observer> meter);

    // The four failure counters below are written by hand rather than with the [Counter<int>] attribute.
    // The generator emits Meter.CreateCounter<T>(name, description) and the second positional parameter of
    // that method is the unit, so the description ends up as the unit and the description stays empty.
    // Exporters that put the unit in the metric name (the Prometheus exporters do) then produce names such as
    // chronicle_observer_partitions_failed_Number_of_failed_partitions_..._total with an empty HELP.
    // Remove this, and use the attribute again, once Cratis/Fundamentals#1138 is fixed and consumed.
    //
    // A counter is only recorded when something happens, so an observer has a series on these instruments from its
    // first failure, and never before. Recording a zero when the observer activates would give every observer
    // instance a series on each of the four, whether it ever fails or not, which spends the cardinality limit on
    // healthy observers (see OpenTelemetryConfigurationExtensions).

    /// <summary>
    /// Records a failed handling attempt for a partition of the observer.
    /// </summary>
    /// <param name="meter">The <see cref="IMeterScope{T}"/> of the observer.</param>
    internal static void PartitionFailed(this IMeterScope<Observer> meter) => meter.Increment(_ => _.PartitionFailed);

    /// <summary>
    /// Records that a failed partition of the observer was evaluated for retry.
    /// </summary>
    /// <param name="meter">The <see cref="IMeterScope{T}"/> of the observer.</param>
    internal static void PartitionRetryAttempt(this IMeterScope<Observer> meter) => meter.Increment(_ => _.PartitionRetryAttempt);

    /// <summary>
    /// Records that a partition of the observer ran out of retry attempts and was quarantined.
    /// </summary>
    /// <param name="meter">The <see cref="IMeterScope{T}"/> of the observer.</param>
    internal static void PartitionQuarantined(this IMeterScope<Observer> meter) => meter.Increment(_ => _.PartitionQuarantined);

    /// <summary>
    /// Records that the quarantine of a partition of the observer was cleared by an operator.
    /// </summary>
    /// <param name="meter">The <see cref="IMeterScope{T}"/> of the observer.</param>
    internal static void PartitionQuarantineCleared(this IMeterScope<Observer> meter) => meter.Increment(_ => _.PartitionQuarantineCleared);

    /// <summary>
    /// Records that the observer was quarantined.
    /// </summary>
    /// <param name="meter">The <see cref="IMeterScope{T}"/> of the observer.</param>
    internal static void ObserverQuarantined(this IMeterScope<Observer> meter) => meter.Increment(_ => _.ObserverQuarantined);

    static void Increment(this IMeterScope<Observer> meter, Func<FailureCounters, Counter<int>> counter)
    {
        var counters = GetFailureCounters(meter);
        if (counters is null)
        {
            return;
        }

        var tags = default(TagList);
        foreach (var (key, tag) in meter.Tags)
        {
            tags.Add(key, tag);
        }

        counter(counters).Add(1, tags);
    }

    /// <summary>
    /// Gets the failure counters, creating them once on the first meter that is used, the way the generated metrics do.
    /// </summary>
    /// <param name="meter">The <see cref="IMeterScope{T}"/> of the observer.</param>
    /// <returns>The <see cref="FailureCounters"/>, or null if there is no meter to create them on.</returns>
    static FailureCounters? GetFailureCounters(IMeterScope<Observer> meter)
    {
        var counters = Volatile.Read(ref _failureCounters);
        if (counters is not null || meter.Meter is null)
        {
            return counters;
        }

        lock (_failureCountersLock)
        {
            return _failureCounters ??= new FailureCounters(meter.Meter);
        }
    }

    sealed class FailureCounters(Meter meter)
    {
        public Counter<int> PartitionFailed { get; } = meter.CreateCounter<int>(PartitionFailedName, unit: null, description: PartitionFailedDescription);

        public Counter<int> PartitionRetryAttempt { get; } = meter.CreateCounter<int>(PartitionRetryAttemptName, unit: null, description: PartitionRetryAttemptDescription);

        public Counter<int> PartitionQuarantined { get; } = meter.CreateCounter<int>(PartitionQuarantinedName, unit: null, description: PartitionQuarantinedDescription);

        public Counter<int> PartitionQuarantineCleared { get; } = meter.CreateCounter<int>(PartitionQuarantineClearedName, unit: null, description: PartitionQuarantineClearedDescription);

        public Counter<int> ObserverQuarantined { get; } = meter.CreateCounter<int>(ObserverQuarantinedName, unit: null, description: ObserverQuarantinedDescription);
    }
}

internal static class ObserverMetricsScopes
{
    internal static IMeterScope<Observer> BeginObserverScope(this IMeter<Observer> meter, ObserverId observerId, ObserverKey observerKey) =>
        meter.BeginScope(new Dictionary<string, object>
        {
            ["ObserverId"] = observerId,
            ["EventStore"] = observerKey.EventStore,
            ["Namespace"] = observerKey.Namespace,
            ["EventSequenceId"] = observerKey.EventSequenceId
        });
}
