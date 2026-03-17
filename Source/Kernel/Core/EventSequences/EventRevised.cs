// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;

namespace Cratis.Chronicle.Events.EventSequences;

/// <summary>
/// System event appended to the System event sequence when an event is revised.
/// Its purpose is to record that an event at a specific sequence number in a specific event sequence
/// was revised, along with the revising content.
/// A reactor handles this event and performs the actual in-place update (pushing to the
/// <c>Revisions</c> array of the original event in storage without altering its type).
/// The revision audit context (who, when, correlation) is carried in the event's <see cref="EventContext"/>.
/// </summary>
/// <param name="Sequence"><see cref="EventSequenceId"/> the event to revise is in.</param>
/// <param name="SequenceNumber"><see cref="EventSequenceNumber"/> of the event to revise.</param>
/// <param name="EventType">The <see cref="EventType"/> of the event being revised.</param>
/// <param name="Content">The revising event content as a JSON string.</param>
[EventType, AllEventStores]
public record EventRevised(
    EventSequenceId Sequence,
    EventSequenceNumber SequenceNumber,
    EventType EventType,
    string Content);
