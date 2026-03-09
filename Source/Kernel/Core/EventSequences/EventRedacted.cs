// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Auditing;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Identities;

namespace Cratis.Chronicle.Events.EventSequences;

/// <summary>
/// The system event appended when an event is redacted. A reactor handles this and performs the actual in-place replacement.
/// </summary>
/// <param name="Sequence"><see cref="EventSequenceId"/> the event was in.</param>
/// <param name="SequenceNumber"><see cref="EventSequenceNumber"/> of the event that was redacted.</param>
/// <param name="Reason"><see cref="RedactionReason"/> representing why it was redacted.</param>
/// <param name="CorrelationId">The <see cref="CorrelationId"/> for the redaction.</param>
/// <param name="Causation">The chain of <see cref="Causation"/> for the redaction.</param>
/// <param name="CausedBy">The <see cref="Identity"/> that caused the redaction.</param>
[EventType, AllEventStores]
public record EventRedacted(
    EventSequenceId Sequence,
    EventSequenceNumber SequenceNumber,
    RedactionReason Reason,
    CorrelationId CorrelationId,
    IEnumerable<Causation> Causation,
    Identity CausedBy);
