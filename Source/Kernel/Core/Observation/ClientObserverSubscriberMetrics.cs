// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.Metrics;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Diagnostics;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Records bounded-cardinality client-delivery fence measurements with a fixed naming mode.
/// </summary>
internal sealed class ClientObserverSubscriberMetrics
{
    static readonly Meter _meter = new(WellKnown.MeterName);
    readonly MetricInstrument<long, CounterMetricRecorder<long>> _lateResults;
    readonly MetricInstrument<long, CounterMetricRecorder<long>> _holdExpirations;
    readonly MetricInstrument<double, HistogramMetricRecorder<double>> _fenceDuration;

    /// <summary>Initializes a new instance of the <see cref="ClientObserverSubscriberMetrics"/> class.</summary>
    /// <param name="names">The naming mode.</param>
    internal ClientObserverSubscriberMetrics(MetricNames names)
    {
        var metrics = new MetricSet(_meter, names);
        _lateResults = metrics.Counter<long>(WellKnownMetricNames.AbandonedDeliveryLateResults, WellKnownMetricNames.AbandonedDeliveryLateResultsUnit, "Client results received after the delivery deadline", WellKnownTelemetryAttributes.AbandonedDeliveryResultState);
        _holdExpirations = metrics.Counter<long>(WellKnownMetricNames.AbandonedDeliveryHoldExpirations, WellKnownMetricNames.AbandonedDeliveryHoldExpirationsUnit, "Client deliveries released by the maximum hold");
        _fenceDuration = metrics.Histogram<double>(WellKnownMetricNames.AbandonedDeliveryFenceDuration, WellKnownMetricNames.AbandonedDeliveryFenceDurationUnit, "Time an abandoned client delivery remained held");
    }

    /// <summary>Counts a late result by state.</summary>
    /// <param name="state">The result state.</param>
    internal void LateResult(ObserverSubscriberState state) => _lateResults.Record(1, state.ToString());

    /// <summary>Counts a maximum-hold expiration.</summary>
    internal void HoldExpired() => _holdExpirations.Record(1);

    /// <summary>Records the actual hold duration.</summary>
    /// <param name="duration">The elapsed hold.</param>
    internal void FenceDuration(TimeSpan duration) => _fenceDuration.Record(Math.Max(0, duration.TotalSeconds));
}
