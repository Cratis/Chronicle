// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences;

/// <summary>
/// Represents the unique identifier of an event sequence.
/// </summary>
/// <param name="Value">Actual value.</param>
public record EventSequenceId(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// The string value for the unspecified event sequence identifier.
    /// </summary>
    public const string UnspecifiedId = "[unspecified]";

    /// <summary>
    /// The string value for the default event log sequence identifier.
    /// </summary>
    public const string LogId = "event-log";

    /// <summary>
    /// The string value for the system event sequence identifier.
    /// </summary>
    public const string SystemId = "system";

    /// <summary>
    /// The string value for the outbox event sequence identifier.
    /// </summary>
    public const string OutboxId = "outbox";

    /// <summary>
    /// The string value for the inbox event sequence identifier.
    /// </summary>
    public const string InboxId = "inbox";

    /// <summary>
    /// The prefix used for per-source inbox event sequence identifiers.
    /// </summary>
    public const string InboxPrefix = "inbox-";

    /// <summary>
    /// The <see cref="EventSequenceId"/> representing an unspecified value.
    /// </summary>
    public static readonly EventSequenceId Unspecified = UnspecifiedId;

    /// <summary>
    /// The <see cref="EventSequenceId"/> representing the event sequence for the default log.
    /// </summary>
    public static readonly EventSequenceId Log = LogId;

    /// <summary>
    /// The <see cref="EventSequenceId"/> representing the system event sequence.
    /// </summary>
    public static readonly EventSequenceId System = SystemId;

    /// <summary>
    /// Gets <see cref="EventSequenceId"/> of the default outbox.
    /// </summary>
    public static readonly EventSequenceId Outbox = OutboxId;

    /// <summary>
    /// Gets the virtual <see cref="EventSequenceId"/> representing all inboxes.
    /// </summary>
    public static readonly EventSequenceId Inbox = InboxId;

    /// <summary>
    /// Get whether or not this is the default log event sequence.
    /// </summary>
    public bool IsEventLog => this == Log;

    /// <summary>
    /// Implicitly convert from a string to <see cref="EventSequenceId"/>.
    /// </summary>
    /// <param name="id">String to convert from.</param>
    public static implicit operator EventSequenceId(string id) => new(id);
}
