// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;

namespace Cratis.Chronicle.Events.EventSequences;

/// <summary>
/// System event appended to the System event sequence when an event is compensated.
/// Its purpose is purely to record that an event at a specific sequence number in a specific event sequence was compensated.
/// A reactor handles this event and performs the actual in-place update.
/// The compensation audit context (who, when, correlation) is carried in the event's <see cref="EventContext"/>.
/// </summary>
/// <param name="Sequence"><see cref="EventSequenceId"/> the event to compensate is in.</param>
/// <param name="SequenceNumber"><see cref="EventSequenceNumber"/> of the event to compensate.</param>
/// <param name="EventType">The <see cref="EventType"/> of the event being compensated.</param>
/// <param name="Content">The compensating event content as a JSON string.</param>
[EventType, AllEventStores]
public record EventCompensationRequested(
    EventSequenceId Sequence,
    EventSequenceNumber SequenceNumber,
    EventType EventType,
    string Content);
