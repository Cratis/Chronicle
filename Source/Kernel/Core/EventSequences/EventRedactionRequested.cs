// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;

namespace Cratis.Chronicle.Events.EventSequences;

/// <summary>
/// System event appended to the System event sequence when a single event is redacted.
/// Its purpose is purely to record that an event at a specific sequence number in a specific event sequence was redacted.
/// A reactor handles this event and performs the actual in-place replacement.
/// The redaction audit context (who, when, correlation) is carried in the event's <see cref="EventContext"/>.
/// </summary>
/// <param name="Sequence"><see cref="EventSequenceId"/> the event to redact is in.</param>
/// <param name="SequenceNumber"><see cref="EventSequenceNumber"/> of the event to redact.</param>
/// <param name="Reason"><see cref="RedactionReason"/> representing why it was redacted.</param>
[EventType, AllEventStores]
public record EventRedactionRequested(
    EventSequenceId Sequence,
    EventSequenceNumber SequenceNumber,
    RedactionReason Reason);
