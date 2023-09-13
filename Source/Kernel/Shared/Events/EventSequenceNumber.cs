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
    /// Gets the sequence number representing the warm up event.
    /// </summary>
    public static readonly EventSequenceNumber WarmUp = ulong.MaxValue;

    /// <summary>
    /// Gets the first sequence number.
    /// </summary>
    public static readonly EventSequenceNumber First = 0u;

    /// <summary>
    /// Gets the max sequence number.
    /// </summary>
    public static readonly EventSequenceNumber Max = ulong.MaxValue - 1;

    /// <summary>
    /// Gets the value when the sequence number is unavailable.
    /// </summary>
    public static readonly EventSequenceNumber Unavailable = ulong.MaxValue - 2;

    /// <summary>
    /// Check if the <see cref="EventSequenceNumber"/> is an actual value representing a sequence number.
    /// </summary>
    /// <returns>True if it can, false if not.</returns>
    /// <remarks>
    /// Values such as <see cref="Unavailable"/>, <see cref="Max"/> and <see cref="WarmUp"/> are not actual values.
    /// They are system values used for special purposes.
    /// </remarks>
    public bool IsActualValue => this != Unavailable && this != Max && this != WarmUp;

    /// <summary>
    /// Check if the <see cref="EventSequenceNumber"/> is unavailable.
    /// </summary>
    public bool IsUnavailable => this == Unavailable;

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
    public static EventSequenceNumber operator +(EventSequenceNumber left, ulong right) => left.IsActualValue ? new(left.Value + right) : left;

    /// <summary>
    /// Adds a event sequence number with a value.
    /// </summary>
    /// <param name="left"><see cref="EventSequenceNumber"/> to subtract from.</param>
    /// <param name="right">Value to add.</param>
    /// <returns>new event sequence number.</returns>
    public static EventSequenceNumber operator -(EventSequenceNumber left, ulong right) => left.IsActualValue ? new(left.Value - right) : left;

    /// <summary>
    /// Adds a event sequence number with a value.
    /// </summary>
    /// <param name="left"><see cref="EventSequenceNumber"/> to add from.</param>
    /// <param name="right">Value to add.</param>
    /// <returns>new event sequence number.</returns>
    public static EventSequenceNumber operator +(EventSequenceNumber left, int right) => left.IsActualValue ? new(left.Value + (ulong)right) : left;

    /// <summary>
    /// Adds a event sequence number with a value.
    /// </summary>
    /// <param name="left"><see cref="EventSequenceNumber"/> to subtract from.</param>
    /// <param name="right">Value to add.</param>
    /// <returns>new event sequence number.</returns>
    public static EventSequenceNumber operator -(EventSequenceNumber left, int right) => left.IsActualValue ? new(left.Value - (ulong)right) : left;

    /// <summary>
    /// Adds a event sequence number with another event sequence number.
    /// </summary>
    /// <param name="left"><see cref="EventSequenceNumber"/> to add from.</param>
    /// <param name="right"><see cref="EventSequenceNumber"/> to add.</param>
    /// <returns>new event sequence number.</returns>
    public static EventSequenceNumber operator +(EventSequenceNumber left, EventSequenceNumber right) => left.IsActualValue ? new(left.Value + right.Value) : left;

    /// <summary>
    /// Adds a event sequence number with a value.
    /// </summary>
    /// <param name="left"><see cref="EventSequenceNumber"/> to add from.</param>
    /// <param name="right"><see cref="EventSequenceNumber"/> to subtract.</param>
    /// <returns>new event sequence number.</returns>
    public static EventSequenceNumber operator -(EventSequenceNumber left, EventSequenceNumber right) => left.IsActualValue ? new(left.Value - right.Value) : left;

    /// <summary>
    /// Get the next <see cref="EventSequenceNumber"/>.
    /// </summary>
    /// <returns>The next <see cref="EventSequenceNumber"/>.</returns>
    public EventSequenceNumber Next() => IsActualValue ? this + 1 : this;
}
