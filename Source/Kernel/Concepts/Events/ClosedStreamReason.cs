// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Events;

/// <summary>
/// Represents the reason for why a <see cref="EventStreamId"/> has been closed.
/// </summary>
/// <param name="Value">Actual value.</param>
public record ClosedStreamReason(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Gets the representation of an unspecified <see cref="ClosedStreamReason"/>.
    /// </summary>
    public static readonly string Unspecified = "Unspecified";

    /// <summary>
    /// Implicitly convert from <see cref="string"/> to <see cref="ClosedStreamReason"/>.
    /// </summary>
    /// <param name="reason"><see cref="string"/> to convert from.</param>
    /// <returns>A converted <see cref="ClosedStreamReason"/>.</returns>;
    public static implicit operator ClosedStreamReason(string reason) => new(reason);
}
