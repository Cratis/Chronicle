// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Events;

/// <summary>
/// Represents the sequence number within an event log for an event.
/// </summary>
/// <param name="Value">The sequence number.</param>
public record EventSequenceNumber(ulong Value) : ConceptAs<ulong>(Value)
{
    /// <summary>
    /// Gets the first sequence number.
    /// </summary>
    public static readonly EventSequenceNumber First = 0u;

    /// <summary>
    /// Gets the value when the sequence number is unavailable.
    /// </summary>
    public static readonly EventSequenceNumber Unavailable = ulong.MaxValue;

    /// <summary>
    /// Implicitly convert from <see cref="ulong"/> to <see cref="EventSequenceNumber"/>.
    /// </summary>
    /// <param name="value">Value to convert from.</param>
    /// <returns>A converted <see cref="EventSequenceNumber"/>.</returns>;
    public static implicit operator EventSequenceNumber(ulong value) => new(value);

    /// <summary>
    /// Adds a event sequence number with a value.
    /// </summary>
    /// <param name="left"><see cref="EventSequenceNumber"/> to add from.</param>
    /// <param name="right">Value to add.</param>
    /// <returns>new event sequence number.</returns>
    public static EventSequenceNumber operator +(EventSequenceNumber left, ulong right) => new(left.Value + right);

    /// <summary>
    /// Adds a event sequence number with a value.
    /// </summary>
    /// <param name="left"><see cref="EventSequenceNumber"/> to subtract from.</param>
    /// <param name="right">Value to add.</param>
    /// <returns>new event sequence number.</returns>
    public static EventSequenceNumber operator -(EventSequenceNumber left, ulong right) => new(left.Value - right);

    /// <summary>
    /// Adds a event sequence number with a value.
    /// </summary>
    /// <param name="left"><see cref="EventSequenceNumber"/> to add from.</param>
    /// <param name="right">Value to add.</param>
    /// <returns>new event sequence number.</returns>
    public static EventSequenceNumber operator +(EventSequenceNumber left, int right) => new(left.Value + (ulong)right);

    /// <summary>
    /// Adds a event sequence number with a value.
    /// </summary>
    /// <param name="left"><see cref="EventSequenceNumber"/> to subtract from.</param>
    /// <param name="right">Value to add.</param>
    /// <returns>new event sequence number.</returns>
    public static EventSequenceNumber operator -(EventSequenceNumber left, int right) => new(left.Value - (ulong)right);

    /// <summary>
    /// Adds a event sequence number with another event sequence number.
    /// </summary>
    /// <param name="left"><see cref="EventSequenceNumber"/> to add from.</param>
    /// <param name="right"><see cref="EventSequenceNumber"/> to add.</param>
    /// <returns>new event sequence number.</returns>
    public static EventSequenceNumber operator +(EventSequenceNumber left, EventSequenceNumber right) => new(left.Value + right.Value);

    /// <summary>
    /// Adds a event sequence number with a value.
    /// </summary>
    /// <param name="left"><see cref="EventSequenceNumber"/> to add from.</param>
    /// <param name="right"><see cref="EventSequenceNumber"/> to subtract.</param>
    /// <returns>new event sequence number.</returns>
    public static EventSequenceNumber operator -(EventSequenceNumber left, EventSequenceNumber right) => new(left.Value - right.Value);
}
