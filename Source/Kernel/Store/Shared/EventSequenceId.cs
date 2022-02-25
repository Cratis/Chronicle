// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Events.Store;

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
    /// Implicitly convert from a string representation of a <see cref="Guid"/> to <see cref="EventSequenceId"/>.
    /// </summary>
    /// <param name="id"><see cref="Guid"/> to convert from.</param>
    public static implicit operator EventSequenceId(string id) => new(Guid.Parse(id));

    /// <summary>
    /// Implicitly convert from <see cref="Guid"/> to <see cref="EventSequenceId"/>.
    /// </summary>
    /// <param name="id"><see cref="Guid"/> to convert from.</param>
    public static implicit operator EventSequenceId(Guid id) => new(id);

    /// <summary>
    /// Get whether or not this is the default log event sequence.
    /// </summary>
    public bool IsEventLog => this == Log;

    /// <summary>
    /// Get whether or not this is the default outbox event sequence.
    /// </summary>
    public bool IsOutbox => this == Outbox;
}
