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
    /// Gets or sets the exclusive position through which constraint index updates have been settled, or null before coverage was recorded.
    /// </summary>
    public EventSequenceNumber? ConstraintIndexWatermark { get; set; }

    /// <summary>
    /// Gets or sets the exclusive boundary of pending unique-constraint history catch-up.
    /// </summary>
    public EventSequenceNumber? ConstraintIndexCatchUpBelow { get; set; }

    /// <summary>
    /// Gets or sets the constraints whose catch-up writes currently require append refusal, or null when none do.
    /// </summary>
    public IEnumerable<Concepts.Events.Constraints.ConstraintName>? ConstraintIndexCatchUpRefusing { get; set; }

    /// <summary>
    /// Gets or sets the last event sequence number for the last event that was appended.
    /// </summary>
    public IDictionary<EventTypeId, EventSequenceNumber> TailSequenceNumberPerEventType { get; set; } = new Dictionary<EventTypeId, EventSequenceNumber>();
}
