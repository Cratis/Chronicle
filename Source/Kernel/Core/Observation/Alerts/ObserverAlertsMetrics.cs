// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Diagnostics;
using Cratis.Metrics;

namespace Cratis.Chronicle.Observation.Alerts;

/// <summary>
/// Counts failed attempts to record observer alert transitions, with instruments and tags cached at activation.
/// </summary>
/// <param name="meter">The meter.</param>
/// <param name="key">The observer key.</param>
/// <param name="names">The naming mode.</param>
internal sealed class ObserverAlertsMetrics(IMeter<ObserverAlerts> meter, ObserverKey key, MetricNames names)
{
    readonly MetricInstrument<int, CounterMetricRecorder<int>> _failures = new MetricSet(
        meter.ActualMeter,
        names,
        [
            new(WellKnownTelemetryAttributes.ObserverId, key.ObserverId),
            new(WellKnownTelemetryAttributes.EventStoreName, key.EventStore),
            new(WellKnownTelemetryAttributes.EventStoreNamespace, key.Namespace),
            new(WellKnownTelemetryAttributes.EventSequenceId, key.EventSequenceId)
        ]).Counter<int>(WellKnownMetricNames.AlertFailedTransitions, WellKnownMetricNames.AlertFailedTransitionsUnit, "Number of failed attempts to record observer alert transitions");

    /// <summary>Records a real failed transition, never an initial zero.</summary>
    internal void TransitionFailed() => _failures.Record(1);
}
