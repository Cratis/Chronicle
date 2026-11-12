// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Diagnostics;
using Cratis.Chronicle.Observation.Claims;

namespace Cratis.Chronicle.Observation;

#pragma warning disable SA1600 // Elements should be documented
#pragma warning disable MA0048 // File name must match type name
#pragma warning disable SA1402 // File may only contain a single type

/// <summary>
/// The metric scope of one observer activation.
/// </summary>
/// <remarks>
/// Every instrument is created the first time the observer records it. Most activations record few or none of them,
/// and each resident observer would otherwise hold all of them, with their tags, for as long as it is active.
/// Activations run one turn at a time, so the instruments are created without synchronization.
/// </remarks>
/// <param name="metrics">The metric set holding the observer's meter, naming mode and tags.</param>
internal sealed class ObserverMetrics(MetricSet metrics) : IDisposable
{
    MetricInstrument<int, CounterMetricRecorder<int>>? _successful;
    MetricInstrument<int, CounterMetricRecorder<int>>? _failed;
    MetricInstrument<int, CounterMetricRecorder<int>>? _retryAttempt;
    MetricInstrument<int, CounterMetricRecorder<int>>? _partitionQuarantined;
    MetricInstrument<int, CounterMetricRecorder<int>>? _quarantineCleared;
    MetricInstrument<int, CounterMetricRecorder<int>>? _observerQuarantined;
    MetricInstrument<int, CounterMetricRecorder<int>>? _skipped;
    MetricInstrument<int, CounterMetricRecorder<int>>? _zeroWorkCatchup;
    MetricInstrument<int, CounterMetricRecorder<int>>? _claimDivergence;
    MetricInstrument<int, CounterMetricRecorder<int>>? _probeScheduled;
    MetricInstrument<int, CounterMetricRecorder<int>>? _probeAttempt;
    MetricInstrument<int, CounterMetricRecorder<int>>? _probeFailure;
    MetricInstrument<int, CounterMetricRecorder<int>>? _probeSuccess;
    MetricInstrument<int, CounterMetricRecorder<int>>? _probeResumed;
    MetricInstrument<int, CounterMetricRecorder<int>>? _probeDeferred;
    MetricInstrument<int, CounterMetricRecorder<int>>? _probeSkipped;
    MetricInstrument<int, CounterMetricRecorder<int>>? _probeCancelled;

    public void Dispose()
    {
        // Instruments belong to the injected meter, not to an observer activation.
    }

    internal void SuccessfulObservation(bool replay) =>
        (_successful ??= ObserverObservationMetrics.SuccessfulObservations(metrics)).Record(1, ObserverObservationMetrics.ReplayValue(replay));

    internal void PartitionFailed() =>
        (_failed ??= metrics.Counter<int>(WellKnownMetricNames.ObserverPartitionFailures, WellKnownMetricNames.ObserverPartitionFailuresUnit, "Number of failed handling attempts for partitions per observer in a given event store and namespace. Every failure is counted, including failures of retries, so the same partition counts once per failed attempt. Replaces the legacy partition retry attempts counter, which repeated it exactly")).Record(1);

    internal void LegacyPartitionRetryAttempt() =>
        (_retryAttempt ??= metrics.PartitionRetryAttempts()).Record(1);

    internal void PartitionQuarantined() =>
        (_partitionQuarantined ??= metrics.Counter<int>(WellKnownMetricNames.ObserverPartitionsQuarantined, WellKnownMetricNames.ObserverPartitionsQuarantinedUnit, "Number of partitions per observer that ran out of retry attempts and were quarantined. Does not count the observer itself being quarantined, see cratis.chronicle.observer.quarantines")).Record(1);

    internal void PartitionQuarantineCleared() =>
        (_quarantineCleared ??= metrics.Counter<int>(WellKnownMetricNames.ObserverPartitionQuarantinesCleared, WellKnownMetricNames.ObserverPartitionQuarantinesClearedUnit, "Number of times an operator cleared the quarantine of a partition per observer, which also resets the retry budget of the partition")).Record(1);

    internal void ObserverQuarantined() =>
        (_observerQuarantined ??= metrics.Counter<int>(WellKnownMetricNames.ObserverQuarantines, WellKnownMetricNames.ObserverQuarantinesUnit, "Number of times an observer was quarantined")).Record(1);

    internal void EventsSuspectedSkipped(int count) =>
        (_skipped ??= metrics.Counter<int>(WellKnownMetricNames.ObserverSuspectedSkippedEvents, WellKnownMetricNames.ObserverSuspectedSkippedEventsUnit, "Number of live events an observer's position moved past without the observer seeing any delivery path cover them. A suspicion, not proof")).Record(count);

    internal void ZeroWorkCatchUp() =>
        (_zeroWorkCatchup ??= metrics.Counter<int>(WellKnownMetricNames.ObserverZeroWorkCatchUps, WellKnownMetricNames.ObserverZeroWorkCatchUpsUnit, "Number of catch-up jobs per observer that concluded without moving the observer forward: no events handled and nothing read past its position")).Record(1);

    internal void ClaimDivergence(ShadowClaimDivergence divergence) =>
        (_claimDivergence ??= metrics.Counter<int>(WellKnownMetricNames.ObserverClaimDivergences, WellKnownMetricNames.ObserverClaimDivergencesUnit, "Shadow partition-claim divergences from replaying/catching-up sets", WellKnownTelemetryAttributes.ObserverClaimDivergence)).Record(1, divergence.ToString());

    internal void WebhookProbeScheduled() =>
        (_probeScheduled ??= metrics.Counter<int>(WellKnownMetricNames.WebhookProbesScheduled, WellKnownMetricNames.WebhookProbesScheduledUnit)).Record(1);

    internal void WebhookProbeAttempt() =>
        (_probeAttempt ??= metrics.Counter<int>(WellKnownMetricNames.WebhookProbeAttempts, WellKnownMetricNames.WebhookProbeAttemptsUnit)).Record(1);

    internal void WebhookProbeFailure() =>
        (_probeFailure ??= metrics.Counter<int>(WellKnownMetricNames.WebhookProbeFailures, WellKnownMetricNames.WebhookProbeFailuresUnit)).Record(1);

    internal void WebhookProbeSuccess() =>
        (_probeSuccess ??= metrics.Counter<int>(WellKnownMetricNames.WebhookProbeSuccesses, WellKnownMetricNames.WebhookProbeSuccessesUnit)).Record(1);

    internal void WebhookProbeResumed() =>
        (_probeResumed ??= metrics.Counter<int>(WellKnownMetricNames.WebhookProbeResumes, WellKnownMetricNames.WebhookProbeResumesUnit)).Record(1);

    internal void WebhookProbeDeferred() =>
        (_probeDeferred ??= metrics.Counter<int>(WellKnownMetricNames.WebhookProbesDeferred, WellKnownMetricNames.WebhookProbesDeferredUnit)).Record(1);

    internal void WebhookProbeSkipped() =>
        (_probeSkipped ??= metrics.Counter<int>(WellKnownMetricNames.WebhookProbesSkipped, WellKnownMetricNames.WebhookProbesSkippedUnit)).Record(1);

    internal void WebhookProbeCancelled() =>
        (_probeCancelled ??= metrics.Counter<int>(WellKnownMetricNames.WebhookProbesCancelled, WellKnownMetricNames.WebhookProbesCancelledUnit)).Record(1);
}
