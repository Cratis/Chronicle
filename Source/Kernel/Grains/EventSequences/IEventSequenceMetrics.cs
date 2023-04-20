// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events;

namespace Aksio.Cratis.Kernel.Grains.EventSequences;

/// <summary>
/// Defines a system that can track metrics for event sequences.
/// </summary>
public interface IEventSequenceMetrics
{
    /// <summary>
    /// Track that an event has been appended.
    /// </summary>
    /// <param name="eventSourceId"><see cref="EventSourceId"/> to use for tagging.</param>
    /// <param name="eventName">Name of event to use for tagging.</param>
    void AppendedEvent(EventSourceId eventSourceId, string eventName);

    /// <summary>
    /// Track that an event sequence number was duplicate.
    /// </summary>
    /// <param name="eventSourceId"><see cref="EventSourceId"/> to use for tagging.</param>
    /// <param name="eventName">Name of event to use for tagging.</param>
    void DuplicateEventSequenceNumber(EventSourceId eventSourceId, string eventName);

    /// <summary>
    /// Track that an append failed.
    /// </summary>
    /// <param name="eventSourceId"><see cref="EventSourceId"/> to use for tagging.</param>
    /// <param name="eventName">Name of event to use for tagging.</param>
    void FailedAppending(EventSourceId eventSourceId, string eventName);
}
