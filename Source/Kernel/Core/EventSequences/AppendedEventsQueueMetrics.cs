// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Metrics;

namespace Cratis.Chronicle.EventSequences;

#pragma warning disable SA1600 // Elements should be documented
#pragma warning disable MA0048 // File name must match type name
#pragma warning disable SA1402 // File may only contain a single type

internal static partial class AppendedEventsQueueMetrics
{
    [Counter<int>("chronicle-appended-events-queue-events-enqueued", "Number of events enqueued")]
    internal static partial void EventsEnqueued(this IMeterScope<AppendedEventsQueue> meter, int numberOfEvents);

    [Counter<int>("chronicle-appended-events-queue-events-handled", "Number of events processed")]
    internal static partial void EventsHandled(this IMeterScope<AppendedEventsQueue> meter, int numberOfEvents);

    [Counter<int>("chronicle-appended-events-queue-events-handling-failures", "Number of events that has handling failures")]
    internal static partial void EventsHandlingFailures(this IMeterScope<AppendedEventsQueue> meter);
}

internal static class AppendedEventsQueueMetricsScopes
{
    internal static IMeterScope<AppendedEventsQueue> BeginScope(
        this IMeter<AppendedEventsQueue> meter,
        EventSequenceKey eventSequenceKey,
        AppendedEventsQueueId queueId) =>
        meter.BeginScope(new Dictionary<string, object>
        {
            ["EventStore"] = eventSequenceKey.EventStore,
            ["Namespace"] = eventSequenceKey.Namespace,
            ["EventSequenceId"] = eventSequenceKey.EventSequenceId,
            ["QueueId"] = queueId
        });
}
