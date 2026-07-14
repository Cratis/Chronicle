// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;

namespace AspNetCore;

/// <summary>
/// Represents a single reactor invocation observed by this application instance.
/// </summary>
/// <param name="Occurred">When the invocation happened.</param>
/// <param name="EventSourceId">The event source (partition) the event belongs to.</param>
/// <param name="Event">The type of event that was handled.</param>
/// <param name="Details">Human readable details about the event.</param>
public record ReactorInvocation(DateTimeOffset Occurred, string EventSourceId, string Event, string Details);

/// <summary>
/// Captures reactor invocations for this application instance so the web page can show which
/// instance events are fanned out to.
/// </summary>
public class ReactorInvocationLog
{
    const int MaxEntries = 200;

    readonly ConcurrentQueue<ReactorInvocation> _invocations = new();

    /// <summary>
    /// Gets all recorded invocations, newest first.
    /// </summary>
    public IEnumerable<ReactorInvocation> All => [.. _invocations.Reverse()];

    /// <summary>
    /// Records a reactor invocation.
    /// </summary>
    /// <param name="eventSourceId">The event source (partition) the event belongs to.</param>
    /// <param name="event">The type of event that was handled.</param>
    /// <param name="details">Human readable details about the event.</param>
    public void Record(string eventSourceId, string @event, string details)
    {
        _invocations.Enqueue(new ReactorInvocation(DateTimeOffset.UtcNow, eventSourceId, @event, details));
        while (_invocations.Count > MaxEntries && _invocations.TryDequeue(out _))
        {
        }
    }
}
