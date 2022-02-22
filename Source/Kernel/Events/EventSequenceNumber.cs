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
    /// Implicitly convert from <see cref="ulong"/> to <see cref="EventSequenceNumber"/>.
    /// </summary>
    /// <param name="value">Value to convert from.</param>
    /// <returns>A converted <see cref="EventSequenceNumber"/>.</returns>;
    public static implicit operator EventSequenceNumber(ulong value) => new(value);
}
