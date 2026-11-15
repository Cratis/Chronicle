// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Diagnostics;
using Cratis.Metrics;

namespace Cratis.Chronicle.EventSequences;

/// <summary>Creates cached metrics for an appended events queue.</summary>
internal static class AppendedEventsQueueMetricsScopes
{
    /// <summary>Creates the instruments and tags in the selected naming mode.</summary>
    /// <param name="meter">The meter.</param>
    /// <param name="eventSequenceKey">The event sequence key.</param>
    /// <param name="queueId">The queue identifier.</param>
    /// <param name="names">The naming mode.</param>
    /// <returns>The cached metrics.</returns>
    internal static AppendedEventsQueueMetrics BeginScope(
        this IMeter<AppendedEventsQueue> meter,
        EventSequenceKey eventSequenceKey,
        AppendedEventsQueueId queueId,
        MetricNames names = MetricNames.Legacy) =>
        new(new MetricSet(
            meter.ActualMeter,
            names,
            [
                new(WellKnownTelemetryAttributes.EventStoreName, eventSequenceKey.EventStore),
                new(WellKnownTelemetryAttributes.EventStoreNamespace, eventSequenceKey.Namespace),
                new(WellKnownTelemetryAttributes.EventSequenceId, eventSequenceKey.EventSequenceId),
                new(WellKnownTelemetryAttributes.AppendedEventsQueueId, queueId)
            ]));
}
