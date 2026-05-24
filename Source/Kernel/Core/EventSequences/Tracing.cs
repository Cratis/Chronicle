// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Diagnostics.OpenTelemetry.Tracing;

namespace Cratis.Chronicle.EventSequences;

/// <summary>
/// Extension methods for tracing.
/// </summary>
internal static class Tracing
{
    /// <summary>
    /// Starts tracing for appending a single event.
    /// </summary>
    /// <param name="key">The <see cref="EventSequenceKey"/>.</param>
    /// <param name="eventType">The <see cref="EventType"/> being appended.</param>
    /// <param name="eventSourceType">The <see cref="EventSourceType"/> of the event source.</param>
    /// <param name="eventSourceId">The <see cref="EventSourceId"/> of the event source.</param>
    /// <returns>The started <see cref="Activity"/>.</returns>
    public static Activity? Append(EventSequenceKey key, EventType eventType, EventSourceType eventSourceType, EventSourceId eventSourceId)
    {
        var activity = ChronicleActivity.Source.StartActivity(nameof(Append), ActivityKind.Internal);
        activity?.Tag(key.EventStore);
        activity?.Tag(key.Namespace);
        activity?.Tag(key.EventSequenceId);
        activity?.Tag(eventType);
        activity?.Tag(eventSourceType, eventSourceId);
        return activity;
    }

    /// <summary>
    /// Starts tracing for appending multiple events.
    /// </summary>
    /// <param name="key">The <see cref="EventSequenceKey"/>.</param>
    /// <returns>The started <see cref="Activity"/>.</returns>
    public static Activity? AppendMany(EventSequenceKey key)
    {
        var activity = ChronicleActivity.Source.StartActivity(nameof(AppendMany), ActivityKind.Internal);
        activity?.Tag(key.EventStore);
        activity?.Tag(key.Namespace);
        activity?.Tag(key.EventSequenceId);
        return activity;
    }

    /// <summary>
    /// Starts tracing for enqueuing events in the appended events queue.
    /// </summary>
    /// <param name="key">The <see cref="EventSequenceKey"/>.</param>
    /// <returns>The started <see cref="Activity"/>.</returns>
    public static Activity? Enqueue(EventSequenceKey key)
    {
        var activity = ChronicleActivity.Source.StartActivity(nameof(Enqueue), ActivityKind.Internal);
        activity?.Tag(key.EventStore);
        activity?.Tag(key.Namespace);
        activity?.Tag(key.EventSequenceId);
        return activity;
    }

    /// <summary>
    /// Starts tracing for dispatching events to an observer.
    /// </summary>
    /// <param name="observerKey">The <see cref="ObserverKey"/> of the observer receiving the events.</param>
    /// <returns>The started <see cref="Activity"/>.</returns>
    public static Activity? Dispatch(ObserverKey observerKey)
    {
        var activity = ChronicleActivity.Source.StartActivity(nameof(Dispatch), ActivityKind.Internal);
        activity?.Tag(observerKey);
        return activity;
    }
}
