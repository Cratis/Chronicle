// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.EventSequences;

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

    /// <summary>
    /// Gets or sets the set of streams that have been marked as completed. No further events may be appended to a completed stream.
    /// </summary>
    /// <remarks>
    /// The default stream (<see cref="EventStreamType.All"/> paired with the default <see cref="EventStreamId"/>) must never be added here.
    /// </remarks>
    public ISet<CompletedStream> CompletedStreams { get; set; } = new HashSet<CompletedStream>();
}
