// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Diagnostics;
using Cratis.Chronicle.Observation.Claims;

namespace Cratis.Chronicle.Observation.for_ObserverMetrics;

/// <summary>
/// A <see cref="System.Diagnostics.Metrics.Meter"/> returns its existing instrument for an identical create and does not
/// publish it again, so a listener cannot tell whether the observer created a metric twice. The metric set counts the
/// metrics it creates instead.
/// </summary>
public class when_recording_a_metric_again : Specification
{
    const int MetricsInObserverScope = 17;

    readonly System.Diagnostics.Metrics.Meter _meter = new($"record-again-{Guid.NewGuid():N}");
    MetricSet _metricSet;
    ObserverMetrics _metrics;
    int _createdAfterFirstRecords;

    void Establish()
    {
        _metricSet = new(_meter, MetricNames.Both);
        _metrics = new(_metricSet);
        RecordEveryMetric();
        _createdAfterFirstRecords = _metricSet.MetricsCreated;
    }

    void Because() => RecordEveryMetric();

    void Destroy() => _meter.Dispose();

    [Fact] void should_create_each_metric_on_its_first_record() => _createdAfterFirstRecords.ShouldEqual(MetricsInObserverScope);
    [Fact] void should_not_create_any_metric_again() => _metricSet.MetricsCreated.ShouldEqual(MetricsInObserverScope);

    void RecordEveryMetric()
    {
        _metrics.SuccessfulObservation(replay: false);
        _metrics.PartitionFailed();
        _metrics.LegacyPartitionRetryAttempt();
        _metrics.PartitionQuarantined();
        _metrics.PartitionQuarantineCleared();
        _metrics.ObserverQuarantined();
        _metrics.EventsSuspectedSkipped(1);
        _metrics.ZeroWorkCatchUp();
        _metrics.ClaimDivergence(ShadowClaimDivergence.ClaimWithoutDerivedSet);
        _metrics.WebhookProbeScheduled();
        _metrics.WebhookProbeAttempt();
        _metrics.WebhookProbeFailure();
        _metrics.WebhookProbeSuccess();
        _metrics.WebhookProbeResumed();
        _metrics.WebhookProbeDeferred();
        _metrics.WebhookProbeSkipped();
        _metrics.WebhookProbeCancelled();
    }
}
