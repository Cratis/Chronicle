// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Auditing;
using Aksio.Cratis.Identities;

namespace Aksio.Cratis.Events;

/// <summary>
/// Represents the content of a redaction event.
/// </summary>
/// <param name="Reason">The reason for redaction.</param>
/// <param name="OriginalEventType">The original type the redaction is for.</param>
/// <param name="Occurred">The time the redaction occurred.</param>
/// <param name="CorrelationId">The unique identifier used to correlation.</param>
/// <param name="Causation">A collection of <see cref="Causation"/> for what caused the redaction.</param>
/// <param name="CausedBy">Who or what caused the event.</param>
public record RedactionEventContent(
    RedactionReason Reason,
    EventTypeId OriginalEventType,
    DateTimeOffset Occurred,
    CorrelationId CorrelationId,
    IEnumerable<Causation> Causation,
    IEnumerable<IdentityId> CausedBy);
