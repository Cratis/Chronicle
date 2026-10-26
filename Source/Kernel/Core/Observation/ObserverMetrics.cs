// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Diagnostics.Metrics;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Diagnostics;
using Cratis.Chronicle.Diagnostics.OpenTelemetry;
using Cratis.Chronicle.Observation.Claims;
using Cratis.Metrics;

namespace Cratis.Chronicle.Observation;

#pragma warning disable SA1600 // Elements should be documented
#pragma warning disable MA0048 // File name must match type name
#pragma warning disable SA1402 // File may only contain a single type

internal static partial class ObserverMetrics
{
    const string PartitionFailedDescription = "Number of failed handling attempts for partitions per observer in a given event store and namespace. Every failure is counted, including failures of retries, so the same partition counts once per failed attempt. Replaces the legacy partition retry attempts counter, which repeated it exactly";
    const string PartitionQuarantineClearedDescription = "Number of times an operator cleared the quarantine of a partition per observer, which also resets the retry budget of the partition";
    const string PartitionQuarantinedDescription = "Number of partitions per observer that ran out of retry attempts and were quarantined. Does not count the observer itself being quarantined, see cratis.chronicle.observer.quarantines";
    const string ObserverQuarantinedDescription = "Number of times an observer was quarantined";
    const string EventsSuspectedSkippedDescription = "Number of live events an observer's position moved past without the observer seeing any delivery path cover them. A suspicion, not proof";
    const string ZeroWorkCatchUpDescription = "Number of catch-up jobs per observer that concluded without moving the observer forward: no events handled and nothing read past its position";

    static readonly Lock _failureCountersLock = new();
    static FailureCounters? _failureCounters;

    [Counter<int>(WellKnownTelemetryNames.Metrics.ObserverSuccessfulObservations, "Number of successful observations per observer in a given event store and namespace", WellKnownTelemetryNames.Metrics.ObserverSuccessfulObservationsUnit)]
    internal static partial void SuccessfulObservation(this IMeterScope<Observer> meter);

    // These counters are hand-written so their failure-path initialization and tagging stay unchanged.
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
    /// <remarks>
    /// Only the legacy retry attempt series is written. It is not part of the convention names because it repeated the
    /// partition failures exactly, and is written here, at its original point, only so it keeps its values for one minor
    /// release. Removed in the next major version.
    /// </remarks>
    internal static void LegacyPartitionRetryAttempt(this IMeterScope<Observer> meter) =>
        GetFailureCounters(meter)?.LegacyPartitionRetryAttempt.Add(1, MetricTags(meter, legacy: true));

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

    /// <summary>
    /// Records live events the position of the observer moved past without any delivery path covering them.
    /// </summary>
    /// <param name="meter">The <see cref="IMeterScope{T}"/> of the observer.</param>
    /// <param name="count">The number of suspected skipped events.</param>
    internal static void EventsSuspectedSkipped(this IMeterScope<Observer> meter, int count) => meter.Increment(_ => _.EventsSuspectedSkipped, count);

    /// <summary>
    /// Records a catch-up job of the observer that concluded without moving the observer forward.
    /// </summary>
    /// <param name="meter">The <see cref="IMeterScope{T}"/> of the observer.</param>
    internal static void ZeroWorkCatchUp(this IMeterScope<Observer> meter) => meter.Increment(_ => _.ZeroWorkCatchUp);

    /// <summary>
    /// Records a diagnostic disagreement between shadow claims and authoritative sets.
    /// </summary>
    /// <param name="meter">The observer meter scope.</param>
    /// <param name="divergence">The bounded disagreement kind.</param>
    internal static void ClaimDivergence(this IMeterScope<Observer> meter, ShadowClaimDivergence divergence) =>
        meter.Increment(_ => _.ClaimDivergence, divergence: divergence);

    internal static void WebhookProbeScheduled(this IMeterScope<Observer> meter) => meter.Increment(_ => _.WebhookProbeScheduled);

    internal static void WebhookProbeAttempt(this IMeterScope<Observer> meter) => meter.Increment(_ => _.WebhookProbeAttempt);

    internal static void WebhookProbeFailure(this IMeterScope<Observer> meter) => meter.Increment(_ => _.WebhookProbeFailure);

    internal static void WebhookProbeSuccess(this IMeterScope<Observer> meter) => meter.Increment(_ => _.WebhookProbeSuccess);

    internal static void WebhookProbeResumed(this IMeterScope<Observer> meter) => meter.Increment(_ => _.WebhookProbeResumed);

    internal static void WebhookProbeDeferred(this IMeterScope<Observer> meter) => meter.Increment(_ => _.WebhookProbeDeferred);

    internal static void WebhookProbeSkipped(this IMeterScope<Observer> meter) => meter.Increment(_ => _.WebhookProbeSkipped);

    internal static void WebhookProbeCancelled(this IMeterScope<Observer> meter) => meter.Increment(_ => _.WebhookProbeCancelled);

    static void Increment(this IMeterScope<Observer> meter, Func<FailureCounters, Counter<int>> counter, int count = 1, ShadowClaimDivergence? divergence = null)
    {
        var counters = GetFailureCounters(meter);
        if (counters is null)
        {
            return;
        }

        var tags = MetricTags(meter);
        if (divergence is not null)
        {
            tags.Add(WellKnownTelemetryNames.Attributes.ObserverClaimDivergence, divergence.Value.ToString());
        }

        counter(counters).Add(count, tags);
    }

    static TagList MetricTags(IMeterScope<Observer> meter, bool legacy = false)
    {
        var tags = default(TagList);
        string[] excluded = ["Partition", "EventSourceId", "cratis.eventsource.id", WellKnownTelemetryNames.Attributes.EventSourceId];
        foreach (var (key, value) in meter.Tags)
        {
            if (!excluded.Contains(key, StringComparer.OrdinalIgnoreCase))
            {
                tags.Add(legacy ? LegacyTelemetryNames.ToLegacyMetricKey(key) : key, value);
            }
        }

        return tags;
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
        public Counter<int> ClaimDivergence { get; } = meter.CreateCounter<int>(
            WellKnownTelemetryNames.Metrics.ObserverClaimDivergences,
            unit: WellKnownTelemetryNames.Metrics.ObserverClaimDivergencesUnit,
            description: "Shadow partition-claim divergences from replaying/catching-up sets");

        public Counter<int> WebhookProbeDeferred { get; } = meter.CreateCounter<int>(WellKnownTelemetryNames.Metrics.WebhookProbesDeferred, unit: WellKnownTelemetryNames.Metrics.WebhookProbesDeferredUnit);

        public Counter<int> WebhookProbeSkipped { get; } = meter.CreateCounter<int>(WellKnownTelemetryNames.Metrics.WebhookProbesSkipped, unit: WellKnownTelemetryNames.Metrics.WebhookProbesSkippedUnit);

        public Counter<int> WebhookProbeCancelled { get; } = meter.CreateCounter<int>(WellKnownTelemetryNames.Metrics.WebhookProbesCancelled, unit: WellKnownTelemetryNames.Metrics.WebhookProbesCancelledUnit);

        public Counter<int> WebhookProbeScheduled { get; } = meter.CreateCounter<int>(WellKnownTelemetryNames.Metrics.WebhookProbesScheduled, unit: WellKnownTelemetryNames.Metrics.WebhookProbesScheduledUnit);

        public Counter<int> WebhookProbeAttempt { get; } = meter.CreateCounter<int>(WellKnownTelemetryNames.Metrics.WebhookProbeAttempts, unit: WellKnownTelemetryNames.Metrics.WebhookProbeAttemptsUnit);

        public Counter<int> WebhookProbeFailure { get; } = meter.CreateCounter<int>(WellKnownTelemetryNames.Metrics.WebhookProbeFailures, unit: WellKnownTelemetryNames.Metrics.WebhookProbeFailuresUnit);

        public Counter<int> WebhookProbeSuccess { get; } = meter.CreateCounter<int>(WellKnownTelemetryNames.Metrics.WebhookProbeSuccesses, unit: WellKnownTelemetryNames.Metrics.WebhookProbeSuccessesUnit);

        public Counter<int> WebhookProbeResumed { get; } = meter.CreateCounter<int>(WellKnownTelemetryNames.Metrics.WebhookProbeResumes, unit: WellKnownTelemetryNames.Metrics.WebhookProbeResumesUnit);

        public Counter<int> PartitionFailed { get; } = meter.CreateCounter<int>(WellKnownTelemetryNames.Metrics.ObserverPartitionFailures, unit: WellKnownTelemetryNames.Metrics.ObserverPartitionFailuresUnit, description: PartitionFailedDescription);

        public Counter<int> LegacyPartitionRetryAttempt { get; } = meter.CreateCounter<int>(LegacyTelemetryNames.PartitionRetryAttempts, unit: null, description: LegacyTelemetryNames.PartitionRetryAttemptsDescription);

        public Counter<int> PartitionQuarantined { get; } = meter.CreateCounter<int>(WellKnownTelemetryNames.Metrics.ObserverPartitionsQuarantined, unit: WellKnownTelemetryNames.Metrics.ObserverPartitionsQuarantinedUnit, description: PartitionQuarantinedDescription);

        public Counter<int> PartitionQuarantineCleared { get; } = meter.CreateCounter<int>(WellKnownTelemetryNames.Metrics.ObserverPartitionQuarantinesCleared, unit: WellKnownTelemetryNames.Metrics.ObserverPartitionQuarantinesClearedUnit, description: PartitionQuarantineClearedDescription);

        public Counter<int> ObserverQuarantined { get; } = meter.CreateCounter<int>(WellKnownTelemetryNames.Metrics.ObserverQuarantines, unit: WellKnownTelemetryNames.Metrics.ObserverQuarantinesUnit, description: ObserverQuarantinedDescription);

        public Counter<int> ZeroWorkCatchUp { get; } = meter.CreateCounter<int>(WellKnownTelemetryNames.Metrics.ObserverZeroWorkCatchUps, unit: WellKnownTelemetryNames.Metrics.ObserverZeroWorkCatchUpsUnit, description: ZeroWorkCatchUpDescription);

        public Counter<int> EventsSuspectedSkipped { get; } = meter.CreateCounter<int>(WellKnownTelemetryNames.Metrics.ObserverSuspectedSkippedEvents, unit: WellKnownTelemetryNames.Metrics.ObserverSuspectedSkippedEventsUnit, description: EventsSuspectedSkippedDescription);
    }
}

internal static class ObserverMetricsScopes
{
    internal static IMeterScope<Observer> BeginObserverScope(this IMeter<Observer> meter, ObserverId observerId, ObserverKey observerKey) =>
        meter.BeginScope(new Dictionary<string, object>
        {
            [WellKnownTelemetryNames.Attributes.ObserverId] = observerId,
            [WellKnownTelemetryNames.Attributes.EventStoreName] = observerKey.EventStore,
            [WellKnownTelemetryNames.Attributes.EventStoreNamespace] = observerKey.Namespace,
            [WellKnownTelemetryNames.Attributes.EventSequenceId] = observerKey.EventSequenceId
        });
}
