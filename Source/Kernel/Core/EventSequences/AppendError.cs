// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences;

/// <summary>
/// Represents the concept of an error that can occur during appending of events.
/// </summary>@
/// <param name="Value">Actual value.</param>
public record AppendError(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Represents the identifier for an unknown event type.
    /// </summary>
    public static readonly AppendError Unknown = string.Empty;

    /// <summary>
    /// Represents the error for appending a private event type to the outbox, which only accepts public event types.
    /// </summary>
    public static readonly AppendError PrivateEventTypeCannotBeAppendedToOutbox = nameof(PrivateEventTypeCannotBeAppendedToOutbox);

    /// <summary>
    /// Represents the error for appending a public event type to the event log, where public events are produced from private ones rather than appended as facts.
    /// </summary>
    public static readonly AppendError PublicEventTypeCannotBeAppendedToEventLog = nameof(PublicEventTypeCannotBeAppendedToEventLog);

    /// <summary>
    /// Represents the error for a publication identity that was reused for a different immutable intent.
    /// </summary>
    public static readonly AppendError EventPublicationConflict = nameof(EventPublicationConflict);

    /// <summary>
    /// Implicitly convert from string to <see cref="AppendError"/>.
    /// </summary>
    /// <param name="id">String to convert from.</param>
    public static implicit operator AppendError(string id) => new(id);
}
