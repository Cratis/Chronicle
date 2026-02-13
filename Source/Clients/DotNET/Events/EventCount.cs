// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events;

/// <summary>
/// Represents a count of events.
/// </summary>
/// <param name="Value">The actual count.</param>
public record EventCount(ulong Value) : ConceptAs<ulong>(Value)
{
    /// <summary>
    /// The value representing an unlimited count.
    /// </summary>
    public static readonly EventCount Unlimited = ulong.MaxValue;

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
    /// Implicitly convert from <see cref="long"/> to <see cref="EventCount"/>.
    /// </summary>
    /// <param name="value">Value to convert from.</param>
    public static implicit operator EventCount(long value) => new((ulong)value);

    /// <summary>
    /// Implicitly convert from <see cref="int"/> to <see cref="EventCount"/>.
    /// </summary>
    /// <param name="value">Value to convert from.</param>
    public static implicit operator EventCount(int value) => new((ulong)value);
}
