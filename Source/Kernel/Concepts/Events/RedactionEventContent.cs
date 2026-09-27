// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Auditing;
using Cratis.Chronicle.Concepts.Identities;

namespace Cratis.Chronicle.Concepts.Events;

/// <summary>
/// Represents the content of a redaction event.
/// </summary>
/// <param name="Reason">The reason for redaction.</param>
/// <param name="OriginalEventType">The original type the redaction is for.</param>
/// <param name="Occurred">When the original event occurred.</param>
/// <param name="CorrelationId">The original event's correlation identifier.</param>
/// <param name="Causation">The original causation types and times, without property values.</param>
/// <param name="CausedBy">The original caused-by identity chain.</param>
public record RedactionEventContent(
    RedactionReason Reason,
    EventTypeId OriginalEventType,
    DateTimeOffset Occurred,
    CorrelationId CorrelationId,
    IEnumerable<Causation> Causation,
    IEnumerable<IdentityId> CausedBy)
{
    /// <summary>
    /// Creates redaction content from the original event, discarding all causation property values.
    /// </summary>
    /// <param name="reason">The redaction reason.</param>
    /// <param name="originalEventType">The original event type.</param>
    /// <param name="occurred">When the original event occurred.</param>
    /// <param name="correlationId">The original correlation identifier.</param>
    /// <param name="causation">The original causation chain.</param>
    /// <param name="causedBy">The original caused-by identity chain.</param>
    /// <returns>Content with only the auditable, non-property causation metadata.</returns>
    public static RedactionEventContent FromOriginal(
        RedactionReason reason,
        EventTypeId originalEventType,
        DateTimeOffset occurred,
        CorrelationId correlationId,
        IEnumerable<Causation> causation,
        IEnumerable<IdentityId> causedBy) =>
        new(
            reason,
            originalEventType,
            occurred,
            correlationId,
            causation.Select(cause => new Causation(cause.Occurred, cause.Type, new Dictionary<string, string>())).ToArray(),
            causedBy.ToArray());

    /// <summary>
    /// Builds the common stored payload, omitting the causation Properties field entirely.
    /// </summary>
    /// <returns>The redaction payload with camel-cased field names.</returns>
    public ExpandoObject ToPayload()
    {
        var payload = new ExpandoObject();
        var values = (IDictionary<string, object?>)payload;
        values["reason"] = Reason.Value;
        values["originalEventType"] = OriginalEventType.Value;
        values["occurred"] = Occurred;
        values["correlationId"] = CorrelationId.Value;
        values["causation"] = Causation.Select(cause => new { type = cause.Type.Value, occurred = cause.Occurred }).ToArray();
        values["causedBy"] = CausedBy.Select(identity => identity.ToString()).ToArray();
        return payload;
    }
}
