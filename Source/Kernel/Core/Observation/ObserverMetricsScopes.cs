// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Diagnostics;
using Cratis.Metrics;

namespace Cratis.Chronicle.Observation;

/// <summary>Creates cached metrics for an observer activation.</summary>
internal static class ObserverMetricsScopes
{
    /// <summary>Creates the instruments and tags in the selected naming mode.</summary>
    /// <param name="meter">The meter.</param>
    /// <param name="observerId">The observer identifier.</param>
    /// <param name="observerKey">The observer key.</param>
    /// <param name="names">The naming mode.</param>
    /// <returns>The cached metrics.</returns>
    internal static ObserverMetrics BeginObserverScope(this IMeter<Observer> meter, ObserverId observerId, ObserverKey observerKey, MetricNames names = MetricNames.Legacy) =>
        new(new MetricSet(
            meter.ActualMeter,
            names,
            [
                new(WellKnownTelemetryAttributes.ObserverId, observerId),
                new(WellKnownTelemetryAttributes.EventStoreName, observerKey.EventStore),
                new(WellKnownTelemetryAttributes.EventStoreNamespace, observerKey.Namespace),
                new(WellKnownTelemetryAttributes.EventSequenceId, observerKey.EventSequenceId)
            ]));
}
