// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
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
    const string PartitionQuarantinedName = "chronicle-observer-partitions-quarantined";
    const string PartitionQuarantinedDescription = "Number of partitions per observer that ran out of retry attempts and were quarantined. Does not count the observer itself being quarantined, see chronicle-observer-quarantined";
    const string ObserverQuarantinedName = "chronicle-observer-quarantined";
    const string ObserverQuarantinedDescription = "Number of times an observer was quarantined";

    static readonly Lock _initializationLock = new();

    [Counter<int>("chronicle-observer-successful-observations", "Number of successful observations per observer in a given event store and namespace")]
    internal static partial void SuccessfulObservation(this IMeterScope<Observer> meter);

    [Counter<int>(PartitionFailedName, PartitionFailedDescription)]
    internal static partial void PartitionFailed(this IMeterScope<Observer> meter);

    [Counter<int>(PartitionRetryAttemptName, PartitionRetryAttemptDescription)]
    internal static partial void PartitionRetryAttempt(this IMeterScope<Observer> meter);

    [Counter<int>(PartitionQuarantinedName, PartitionQuarantinedDescription)]
    internal static partial void PartitionQuarantined(this IMeterScope<Observer> meter);

    [Counter<int>(ObserverQuarantinedName, ObserverQuarantinedDescription)]
    internal static partial void ObserverQuarantined(this IMeterScope<Observer> meter);

    /// <summary>
    /// Records a zero for the observer failure counters, so their series exist for the observer before its first failure.
    /// </summary>
    /// <param name="meter">The <see cref="IMeterScope{T}"/> of the observer.</param>
    /// <remarks>
    /// A counter series only exists from its first measurement. Without this, an observer's first failure creates
    /// its series at 1, and a rate or increase over a window sees no change since there is nothing to compare to.
    /// The generated methods above create their instruments lazily and keep them in the static fields they declare.
    /// This creates them the same way, with the same name and description, into the same fields, so there is one
    /// instrument per counter regardless of which of the two reaches it first.
    /// </remarks>
    internal static void InitializeFailureCounters(this IMeterScope<Observer> meter)
    {
        if (meter.Meter is null)
        {
            return;
        }

        lock (_initializationLock)
        {
            PartitionFailedMetric ??= meter.Meter.CreateCounter<int>(PartitionFailedName, description: PartitionFailedDescription);
            PartitionRetryAttemptMetric ??= meter.Meter.CreateCounter<int>(PartitionRetryAttemptName, description: PartitionRetryAttemptDescription);
            PartitionQuarantinedMetric ??= meter.Meter.CreateCounter<int>(PartitionQuarantinedName, description: PartitionQuarantinedDescription);
            ObserverQuarantinedMetric ??= meter.Meter.CreateCounter<int>(ObserverQuarantinedName, description: ObserverQuarantinedDescription);
        }

        var tags = default(TagList);
        foreach (var (key, value) in meter.Tags)
        {
            tags.Add(key, value);
        }

        PartitionFailedMetric.Add(0, tags);
        PartitionRetryAttemptMetric.Add(0, tags);
        PartitionQuarantinedMetric.Add(0, tags);
        ObserverQuarantinedMetric.Add(0, tags);
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
