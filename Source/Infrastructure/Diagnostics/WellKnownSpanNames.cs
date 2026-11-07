// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Diagnostics;

/// <summary>
/// The kernel span names.
/// </summary>
public static class WellKnownSpanNames
{
    /// <summary>Appending a single event.</summary>
    public const string EventSequenceAppend = "cratis.chronicle.event_sequence.append";

    /// <summary>Appending many events.</summary>
    public const string EventSequenceAppendMany = "cratis.chronicle.event_sequence.append_many";

    /// <summary>Handling events in an observer.</summary>
    public const string ObserverHandle = "cratis.chronicle.observer.handle";

    /// <summary>Enqueueing events on the appended events queue.</summary>
    public const string AppendedEventsQueueEnqueue = "cratis.chronicle.appended_events_queue.enqueue";

    /// <summary>Dispatching events from the appended events queue.</summary>
    public const string AppendedEventsQueueDispatch = "cratis.chronicle.appended_events_queue.dispatch";
}
