// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Events;

/// <summary>
/// Represents a count of events.
/// </summary>
/// <param name="Value">The actual count.</param>
public record EventCount(ulong Value) : ConceptAs<ulong>(Value)
{
    /// <summary>
    /// The value representing an unset value.
    /// </summary>
    public static readonly EventCount NotSet = ulong.MaxValue;

    /// <summary>
    /// The value representing zero.
    /// </summary>
    public static readonly EventCount Zero = 0L;

    /// <summary>
    /// Implicitly convert from <see cref="ulong"/> to <see cref="EventCount"/>.
    /// </summary>
    /// <param name="value">Value to convert from.</param>
    public static implicit operator EventCount(ulong value) => new(value);

    /// <summary>
    /// Implicitly convert from <see cref="int"/> to <see cref="EventCount"/>.
    /// </summary>
    /// <param name="value">Value to convert from.</param>
    public static implicit operator EventCount(int value) => new(value);

    /// <summary>
    /// Add a <see cref="ulong"/> to the <see cref="EventCount"/>.
    /// </summary>
    /// <param name="left"><see cref="EventCount"/> to add to.</param>
    /// <param name="right">The value to add.</param>
    /// <returns>A new instance with the added value.</returns>
    public static EventCount operator +(EventCount left, ulong right) => new(left.Value + right);

    /// <summary>
    /// Add a <see cref="EventCount"/> to the <see cref="EventCount"/>.
    /// </summary>
    /// <param name="left"><see cref="EventCount"/> to add to.</param>
    /// <param name="right">The <see cref="EventCount"/> to add.</param>
    /// <returns>A new instance with the added value.</returns>
    public static EventCount operator +(EventCount left, EventCount right) => new(left.Value + right.Value);

    /// <summary>
    /// Increase the count.
    /// </summary>
    /// <returns>A new instance with the increased count.</returns>
    public EventCount Increase() => new(Value + 1);
}
