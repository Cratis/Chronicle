// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events;

namespace Aksio.Cratis.Kernel.Grains.EventSequences;

/// <summary>
/// Represents the state used by the event sequence. This state is meant to be per event sequence instance.
/// </summary>
public class EventSequenceState
{
    /// <summary>
    /// Gets or sets the next event sequence number for the next event being appended.
    /// </summary>
    public EventSequenceNumber SequenceNumber { get; set; } = EventSequenceNumber.First;

    /// <summary>
    /// Gets or sets the last event sequence number for the last event that was appended.
    /// </summary>
    public IDictionary<EventTypeId, EventSequenceNumber> TailSequenceNumberPerEventType { get; set; } = new Dictionary<EventTypeId, EventSequenceNumber>();
}
