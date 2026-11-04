// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Diagnostics;
using Cratis.Chronicle.Observation.Claims;

namespace Cratis.Chronicle.Observation;

#pragma warning disable SA1600 // Elements should be documented
#pragma warning disable MA0048 // File name must match type name
#pragma warning disable SA1402 // File may only contain a single type

internal sealed class ObserverMetrics(MetricSet metrics) : IDisposable
{
    readonly MetricInstrument<int, CounterMetricRecorder<int>> _successful = metrics.Counter<int>(WellKnownMetricNames.ObserverSuccessfulObservations, WellKnownMetricNames.ObserverSuccessfulObservationsUnit, "Number of successful observations per observer in a given event store and namespace");
    readonly MetricInstrument<int, CounterMetricRecorder<int>> _failed = metrics.Counter<int>(WellKnownMetricNames.ObserverPartitionFailures, WellKnownMetricNames.ObserverPartitionFailuresUnit, "Number of failed handling attempts for partitions per observer in a given event store and namespace. Every failure is counted, including failures of retries, so the same partition counts once per failed attempt. Replaces the legacy partition retry attempts counter, which repeated it exactly");
    readonly MetricInstrument<int, CounterMetricRecorder<int>> _retryAttempt = metrics.PartitionRetryAttempts();
    readonly MetricInstrument<int, CounterMetricRecorder<int>> _partitionQuarantined = metrics.Counter<int>(WellKnownMetricNames.ObserverPartitionsQuarantined, WellKnownMetricNames.ObserverPartitionsQuarantinedUnit, "Number of partitions per observer that ran out of retry attempts and were quarantined. Does not count the observer itself being quarantined, see cratis.chronicle.observer.quarantines");
    readonly MetricInstrument<int, CounterMetricRecorder<int>> _quarantineCleared = metrics.Counter<int>(WellKnownMetricNames.ObserverPartitionQuarantinesCleared, WellKnownMetricNames.ObserverPartitionQuarantinesClearedUnit, "Number of times an operator cleared the quarantine of a partition per observer, which also resets the retry budget of the partition");
    readonly MetricInstrument<int, CounterMetricRecorder<int>> _observerQuarantined = metrics.Counter<int>(WellKnownMetricNames.ObserverQuarantines, WellKnownMetricNames.ObserverQuarantinesUnit, "Number of times an observer was quarantined");
    readonly MetricInstrument<int, CounterMetricRecorder<int>> _skipped = metrics.Counter<int>(WellKnownMetricNames.ObserverSuspectedSkippedEvents, WellKnownMetricNames.ObserverSuspectedSkippedEventsUnit, "Number of live events an observer's position moved past without the observer seeing any delivery path cover them. A suspicion, not proof");
    readonly MetricInstrument<int, CounterMetricRecorder<int>> _zeroWorkCatchup = metrics.Counter<int>(WellKnownMetricNames.ObserverZeroWorkCatchUps, WellKnownMetricNames.ObserverZeroWorkCatchUpsUnit, "Number of catch-up jobs per observer that concluded without moving the observer forward: no events handled and nothing read past its position");
    readonly MetricInstrument<int, CounterMetricRecorder<int>> _claimDivergence = metrics.Counter<int>(WellKnownMetricNames.ObserverClaimDivergences, WellKnownMetricNames.ObserverClaimDivergencesUnit, "Shadow partition-claim divergences from replaying/catching-up sets", WellKnownTelemetryAttributes.ObserverClaimDivergence);
    readonly MetricInstrument<int, CounterMetricRecorder<int>> _probeScheduled = metrics.Counter<int>(WellKnownMetricNames.WebhookProbesScheduled, WellKnownMetricNames.WebhookProbesScheduledUnit);
    readonly MetricInstrument<int, CounterMetricRecorder<int>> _probeAttempt = metrics.Counter<int>(WellKnownMetricNames.WebhookProbeAttempts, WellKnownMetricNames.WebhookProbeAttemptsUnit);
    readonly MetricInstrument<int, CounterMetricRecorder<int>> _probeFailure = metrics.Counter<int>(WellKnownMetricNames.WebhookProbeFailures, WellKnownMetricNames.WebhookProbeFailuresUnit);
    readonly MetricInstrument<int, CounterMetricRecorder<int>> _probeSuccess = metrics.Counter<int>(WellKnownMetricNames.WebhookProbeSuccesses, WellKnownMetricNames.WebhookProbeSuccessesUnit);
    readonly MetricInstrument<int, CounterMetricRecorder<int>> _probeResumed = metrics.Counter<int>(WellKnownMetricNames.WebhookProbeResumes, WellKnownMetricNames.WebhookProbeResumesUnit);
    readonly MetricInstrument<int, CounterMetricRecorder<int>> _probeDeferred = metrics.Counter<int>(WellKnownMetricNames.WebhookProbesDeferred, WellKnownMetricNames.WebhookProbesDeferredUnit);
    readonly MetricInstrument<int, CounterMetricRecorder<int>> _probeSkipped = metrics.Counter<int>(WellKnownMetricNames.WebhookProbesSkipped, WellKnownMetricNames.WebhookProbesSkippedUnit);
    readonly MetricInstrument<int, CounterMetricRecorder<int>> _probeCancelled = metrics.Counter<int>(WellKnownMetricNames.WebhookProbesCancelled, WellKnownMetricNames.WebhookProbesCancelledUnit);

    public void Dispose()
    {
        // Instruments belong to the injected meter, not to an observer activation.
    }

    internal void SuccessfulObservation() => _successful.Record(1);

    internal void PartitionFailed() => _failed.Record(1);
    internal void LegacyPartitionRetryAttempt() => _retryAttempt.Record(1);
    internal void PartitionQuarantined() => _partitionQuarantined.Record(1);
    internal void PartitionQuarantineCleared() => _quarantineCleared.Record(1);
    internal void ObserverQuarantined() => _observerQuarantined.Record(1);
    internal void EventsSuspectedSkipped(int count) => _skipped.Record(count);
    internal void ZeroWorkCatchUp() => _zeroWorkCatchup.Record(1);
    internal void ClaimDivergence(ShadowClaimDivergence divergence) => _claimDivergence.Record(1, divergence.ToString());
    internal void WebhookProbeScheduled() => _probeScheduled.Record(1);
    internal void WebhookProbeAttempt() => _probeAttempt.Record(1);
    internal void WebhookProbeFailure() => _probeFailure.Record(1);
    internal void WebhookProbeSuccess() => _probeSuccess.Record(1);
    internal void WebhookProbeResumed() => _probeResumed.Record(1);
    internal void WebhookProbeDeferred() => _probeDeferred.Record(1);
    internal void WebhookProbeSkipped() => _probeSkipped.Record(1);
    internal void WebhookProbeCancelled() => _probeCancelled.Record(1);
}
