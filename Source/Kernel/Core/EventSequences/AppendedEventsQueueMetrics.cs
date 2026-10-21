// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Diagnostics.Metrics;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Metrics;

namespace Cratis.Chronicle.EventSequences;

#pragma warning disable SA1600 // Elements should be documented
#pragma warning disable MA0048 // File name must match type name
#pragma warning disable SA1402 // File may only contain a single type

internal static partial class AppendedEventsQueueMetrics
{
    const string SuspectedUndeliveredName = "chronicle-appended-events-queue-suspected-undelivered-events";
    const string SuspectedUndeliveredDescription = "Number of event deliveries to an observer whose dispatch failed; one event can count once per observer";

    static readonly Lock _suspectedUndeliveredLock = new();
    static Counter<int>? _suspectedUndelivered;

    [Counter<int>("chronicle-appended-events-queue-events-enqueued", "Number of events enqueued")]
    internal static partial void EventsEnqueued(this IMeterScope<AppendedEventsQueue> meter, int numberOfEvents);

    [Counter<int>("chronicle-appended-events-queue-events-handled", "Number of events processed")]
    internal static partial void EventsHandled(this IMeterScope<AppendedEventsQueue> meter, int numberOfEvents);

    [Counter<int>("chronicle-appended-events-queue-events-handling-failures", "Number of events that has handling failures")]
    internal static partial void EventsHandlingFailures(this IMeterScope<AppendedEventsQueue> meter);

    [Counter<int>("chronicle-appended-events-queue-events-spilled-to-catchup", "Number of events spilled to observer catch-up when the queue was full")]
    internal static partial void EventsSpilledToCatchup(this IMeterScope<AppendedEventsQueue> meter, int numberOfEvents);

    /// <summary>
    /// Records event deliveries whose dispatch to an observer failed.
    /// </summary>
    /// <param name="meter">The <see cref="IMeterScope{T}"/> of the queue.</param>
    /// <param name="numberOfEvents">The number of events whose dispatch to the observer failed.</param>
    /// <param name="observerId">The <see cref="ObserverId"/> of the observer.</param>
    /// <remarks>
    /// Written by hand rather than with the counter attribute for the same reason as the observer failure counters:
    /// the generator would emit the description as the unit (Cratis/Fundamentals#1138).
    /// </remarks>
    internal static void EventsSuspectedUndelivered(this IMeterScope<AppendedEventsQueue> meter, int numberOfEvents, ObserverId observerId)
    {
        var counter = Volatile.Read(ref _suspectedUndelivered);
        if (counter is null)
        {
            if (meter.Meter is null)
            {
                return;
            }

            lock (_suspectedUndeliveredLock)
            {
                counter = _suspectedUndelivered ??= meter.Meter.CreateCounter<int>(SuspectedUndeliveredName, unit: null, description: SuspectedUndeliveredDescription);
            }
        }

        var tags = default(TagList);
        foreach (var (key, tag) in meter.Tags)
        {
            tags.Add(key, tag);
        }

        tags.Add("ObserverId", observerId);
        counter.Add(numberOfEvents, tags);
    }
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
