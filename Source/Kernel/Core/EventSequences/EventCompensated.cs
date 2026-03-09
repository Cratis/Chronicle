// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Auditing;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Identities;

namespace Cratis.Chronicle.Events.EventSequences;

/// <summary>
/// The system event appended when an event is compensated. A reactor handles this and performs the actual in-place update.
/// </summary>
/// <param name="Sequence"><see cref="EventSequenceId"/> the event was in.</param>
/// <param name="SequenceNumber"><see cref="EventSequenceNumber"/> of the event that was compensated.</param>
/// <param name="EventType">The <see cref="EventType"/> of the event being compensated.</param>
/// <param name="Content">The compensating event content as a JSON string.</param>
/// <param name="CorrelationId">The <see cref="CorrelationId"/> for the compensation.</param>
/// <param name="Causation">The chain of <see cref="Causation"/> for the compensation.</param>
/// <param name="CausedBy">The <see cref="Identity"/> that caused the compensation.</param>
[EventType, AllEventStores]
public record EventCompensated(
    EventSequenceId Sequence,
    EventSequenceNumber SequenceNumber,
    EventType EventType,
    string Content,
    CorrelationId CorrelationId,
    IEnumerable<Causation> Causation,
    Identity CausedBy);
