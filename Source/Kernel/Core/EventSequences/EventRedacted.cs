// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Auditing;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Identities;

namespace Cratis.Chronicle.Events.EventSequences;

/// <summary>
/// Represents the content stored inside a user-sequence event after it has been redacted in-place.
/// When a user-sequence event is redacted, its type is changed to <see cref="GlobalEventTypes.Redaction"/>
/// and its Content field is replaced with the properties of this record, preserving the full original context
/// for audit purposes.
/// </summary>
/// <remarks>
/// This is NOT a Chronicle system event (<c>[EventType]</c> is intentionally absent) — it is a content record
/// stored inside the event document. The event's own context fields (Occurred, CorrelationId, Causation, CausedBy)
/// are updated to reflect the time and identity of the redaction.
/// See also: <see cref="EventRedactionRequested"/>, which is the <c>[EventType]</c> system event that triggers
/// the reactor to perform the actual in-place replacement.
/// </remarks>
/// <param name="Reason">The reason for redaction.</param>
/// <param name="OriginalEventType">The original <see cref="EventTypeId"/> of the event before redaction.</param>
/// <param name="Occurred">The time the ORIGINAL event occurred (preserved for audit).</param>
/// <param name="CorrelationId">The <see cref="CorrelationId"/> of the ORIGINAL event (preserved for audit).</param>
/// <param name="Causation">The causation chain of the ORIGINAL event (preserved for audit).</param>
/// <param name="CausedBy">The identity chain that caused the ORIGINAL event (preserved for audit).</param>
public record EventRedacted(
    RedactionReason Reason,
    EventTypeId OriginalEventType,
    DateTimeOffset Occurred,
    CorrelationId CorrelationId,
    IEnumerable<Causation> Causation,
    IEnumerable<IdentityId> CausedBy);
