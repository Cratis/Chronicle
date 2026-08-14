// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.SequenceQueries;

/// <summary>
/// Represents the display name a user gives a saved event sequence query.
/// </summary>
/// <param name="Value">The underlying value.</param>
public record SequenceQueryName(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Represents an unset <see cref="SequenceQueryName"/>.
    /// </summary>
    public static readonly SequenceQueryName NotSet = new(string.Empty);

    /// <summary>
    /// Implicitly convert from <see cref="string"/> to <see cref="SequenceQueryName"/>.
    /// </summary>
    /// <param name="value">Value to convert from.</param>
    public static implicit operator SequenceQueryName(string value) => new(value);
}
