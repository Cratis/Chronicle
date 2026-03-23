// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;

namespace Cratis.Chronicle.Events.EventSequences;

/// <summary>
/// System event appended to the System event sequence when all events for a given <see cref="EventSourceId"/> are redacted.
/// Its purpose is purely to record that events for a specific event source were redacted from a specific event sequence.
/// A reactor handles this event and performs the actual in-place replacements.
/// The redaction audit context (who, when, correlation) is carried in the event's <see cref="EventContext"/>.
/// </summary>
/// <param name="Sequence"><see cref="EventSequenceId"/> the events to redact are in.</param>
/// <param name="EventSourceId"><see cref="EventSourceId"/> representing which events to redact.</param>
/// <param name="EventTypes">Collection of <see cref="EventType"/> to redact. Empty means all event types for the event source.</param>
/// <param name="Reason"><see cref="RedactionReason"/> representing why they were redacted.</param>
[EventType, AllEventStores]
public record EventsRedactedForEventSource(
    EventSequenceId Sequence,
    EventSourceId EventSourceId,
    IEnumerable<EventType> EventTypes,
    RedactionReason Reason);
