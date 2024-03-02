// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.EventSequences;

/// <summary>
/// Represents the unique identifier of an event sequence.
/// </summary>
/// <param name="Value">Actual value.</param>
public record EventSequenceId(Guid Value) : ConceptAs<Guid>(Value)
{
    /// <summary>
    /// The <see cref="EventSequenceId"/> representing an unspecified value.
    /// </summary>
    public static readonly EventSequenceId Unspecified = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff");

    /// <summary>
    /// The <see cref="EventSequenceId"/> representing the event sequence for the default log.
    /// </summary>
    public static readonly EventSequenceId Log = Guid.Empty;

    /// <summary>
    /// The <see cref="EventSequenceId"/> representing the default outbox.
    /// </summary>
    public static readonly EventSequenceId Outbox = Guid.Parse("ae99de1e-b19f-4a33-a5c4-3908508ce59f");

    /// <summary>
    /// The <see cref="EventSequenceId"/> representing the default inbox.
    /// </summary>
    public static readonly EventSequenceId Inbox = Guid.Parse("2b608a79-77d2-4ccf-af43-4c37dee46592");

    /// <summary>
    /// The name of the sequence representing the system sequence.
    /// </summary>
    /// <remarks>
    /// This is represented as a string name, as part of the transition away from GUIDs representing sequences to strings: https://github.com/aksio-insurtech/Cratis/issues/921.
    /// </remarks>
    public const string System = "system";

    /// <summary>
    /// The <see cref="EventSequenceId"/> representing the default inbox.
    /// </summary>
    public static readonly EventSequenceId SystemId = Guid.Parse("cf3612a4-48fe-462a-af3e-2bd9ad6f6825");

    /// <summary>
    /// Get whether or not this is the default log event sequence.
    /// </summary>
    public bool IsEventLog => this == Log;

    /// <summary>
    /// Get whether or not this is the default outbox event sequence.
    /// </summary>
    public bool IsOutbox => this == Outbox;

    /// <summary>
    /// Get whether or not this is the default outbox event sequence.
    /// </summary>
    public bool IsInbox => this == Inbox;

    /// <summary>
    /// Implicitly convert from a string representation of a <see cref="Guid"/> to <see cref="EventSequenceId"/>.
    /// </summary>
    /// <param name="id"><see cref="Guid"/> to convert from.</param>
    public static implicit operator EventSequenceId(string id)
    {
        // TODO: We're doing this explicit check for well-known type "system" until we have completed the transition from GUIDs to strings: https://github.com/aksio-insurtech/Cratis/issues/921
        if (id == System) return SystemId;
        return new(Guid.Parse(id));
    }

    /// <summary>
    /// Implicitly convert from <see cref="Guid"/> to <see cref="EventSequenceId"/>.
    /// </summary>
    /// <param name="id"><see cref="Guid"/> to convert from.</param>
    public static implicit operator EventSequenceId(Guid id) => new(id);

    /// <summary>
    /// Implicitly convert from bytes representing a <see cref="Guid"/> to <see cref="EventSequenceId"/>.
    /// </summary>
    /// <param name="bytes">Bytes to convert from.</param>
    public static implicit operator EventSequenceId(ReadOnlyMemory<byte> bytes) => new(new Guid(bytes.Span));
}
