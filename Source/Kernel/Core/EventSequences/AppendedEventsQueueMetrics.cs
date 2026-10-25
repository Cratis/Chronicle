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

    const string ObserversSpilledName = "chronicle-appended-events-queue-observers-spilled-to-catchup";
    const string ObserversSpilledDescription = "Number of times an observer was moved from live delivery to catch-up, by reason";

    static readonly Lock _suspectedUndeliveredLock = new();
    static readonly Lock _observersSpilledLock = new();
    static Counter<int>? _suspectedUndelivered;
    static Counter<int>? _observersSpilled;

    [Counter<int>("chronicle-appended-events-queue-events-enqueued", "Number of events enqueued")]
    internal static partial void EventsEnqueued(this IMeterScope<AppendedEventsQueue> meter, int numberOfEvents);

    [Counter<int>("chronicle-appended-events-queue-events-handled", "Number of events processed")]
    internal static partial void EventsHandled(this IMeterScope<AppendedEventsQueue> meter, int numberOfEvents);

    [Counter<int>("chronicle-appended-events-queue-events-handling-failures", "Number of events that has handling failures")]
    internal static partial void EventsHandlingFailures(this IMeterScope<AppendedEventsQueue> meter);

    [Counter<int>("chronicle-appended-events-queue-events-spilled-to-catchup", "Number of events spilled to observer catch-up when the queue was full")]
    internal static partial void EventsSpilledToCatchup(this IMeterScope<AppendedEventsQueue> meter, int numberOfEvents);

    [Counter<int>("chronicle-appended-events-queue-catchup-trigger-budget-exceeded", "Number of spilled observer catch-up triggers still failing after the warning budget")]
    internal static partial void CatchupTriggerBudgetExceeded(this IMeterScope<AppendedEventsQueue> meter);

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

    /// <summary>
    /// Records that an observer was moved from live delivery to its catch-up path.
    /// </summary>
    /// <param name="meter">The <see cref="IMeterScope{T}"/> of the queue.</param>
    /// <param name="observerId">The <see cref="ObserverId"/> of the spilled observer.</param>
    /// <param name="reason">Why it was spilled, one of <see cref="AppendedEventsQueueSpillReasons"/>.</param>
    /// <remarks>
    /// Written by hand for the same reason as <see cref="EventsSuspectedUndelivered"/> (Cratis/Fundamentals#1138).
    /// </remarks>
    internal static void ObserverSpilledToCatchup(this IMeterScope<AppendedEventsQueue> meter, ObserverId observerId, string reason)
    {
        var counter = Volatile.Read(ref _observersSpilled);
        if (counter is null)
        {
            if (meter.Meter is null)
            {
                return;
            }

            lock (_observersSpilledLock)
            {
                counter = _observersSpilled ??= meter.Meter.CreateCounter<int>(ObserversSpilledName, unit: null, description: ObserversSpilledDescription);
            }
        }

        var tags = default(TagList);
        foreach (var (key, tag) in meter.Tags)
        {
            tags.Add(key, tag);
        }

        tags.Add("ObserverId", observerId);
        tags.Add("Reason", reason);
        counter.Add(1, tags);
    }
}

/// <summary>
/// The reasons an observer is spilled from live delivery to catch-up, used as the <c language="csharp">Reason</c> tag of the spill counter.
/// </summary>
internal static class AppendedEventsQueueSpillReasons
{
    /// <summary>The queue was full.</summary>
    internal const string QueueFull = "QueueFull";

    /// <summary>Dispatching to the observer failed.</summary>
    internal const string DispatchFailed = "DispatchFailed";

    /// <summary>The observer's share of one dispatch exceeded the partition run limit.</summary>
    internal const string FragmentedBacklog = "FragmentedBacklog";
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
