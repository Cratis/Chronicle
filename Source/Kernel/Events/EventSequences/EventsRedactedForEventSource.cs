// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Events;
using Cratis.EventSequences;

namespace Cratis.Chronicle.Events.EventSequences;

/// <summary>
/// The event that occurs when a collection of events are redacted based on their <see cref="EventSourceId"/>.
/// </summary>
/// <param name="Sequence"><see cref="EventSequenceId"/> the event was in.</param>
/// <param name="EventSourceId"><see cref="EventSourceId"/> representing the events that was redacted.</param>
/// <param name="EventTypes">Collection of <see cref="EventType"/> to redact.</param>
/// <param name="Reason"><see cref="RedactionReason"/> representing why it was redacted.</param>
public record EventsRedactedForEventSource(
    EventSequenceId Sequence,
    EventSourceId EventSourceId,
    IEnumerable<EventType> EventTypes,
    RedactionReason Reason);
