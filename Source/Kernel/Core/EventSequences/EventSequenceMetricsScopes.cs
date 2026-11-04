// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Diagnostics;
using Cratis.Metrics;

namespace Cratis.Chronicle.EventSequences;

/// <summary>Creates a cached metric set for an event sequence activation.</summary>
internal static class EventSequenceMetricsScopes
{
    /// <summary>Creates the instruments and tags in the selected naming mode.</summary>
    /// <param name="meter">The meter.</param>
    /// <param name="eventStore">The event store.</param>
    /// <param name="namespace">The namespace.</param>
    /// <param name="names">The naming mode.</param>
    /// <returns>The cached metrics.</returns>
    internal static EventSequenceMetrics BeginEventSequenceScope(this IMeter<EventSequence> meter, EventStoreName eventStore, EventStoreNamespaceName @namespace, MetricNames names = MetricNames.Legacy) =>
        new(new MetricSet(
            meter.ActualMeter,
            names,
            [
                new(WellKnownTelemetryAttributes.EventStoreName, eventStore),
                new(WellKnownTelemetryAttributes.EventStoreNamespace, @namespace)
            ]));
}
