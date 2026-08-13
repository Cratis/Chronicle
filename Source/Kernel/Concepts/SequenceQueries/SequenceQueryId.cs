// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.SequenceQueries;

/// <summary>
/// Represents the unique identifier of a saved event sequence query.
/// </summary>
/// <param name="Value">The underlying value.</param>
public record SequenceQueryId(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Represents an unset <see cref="SequenceQueryId"/>.
    /// </summary>
    public static readonly SequenceQueryId NotSet = new(string.Empty);

    /// <summary>
    /// Implicitly convert from <see cref="string"/> to <see cref="SequenceQueryId"/>.
    /// </summary>
    /// <param name="value">Value to convert from.</param>
    public static implicit operator SequenceQueryId(string value) => new(value);

    /// <summary>
    /// Create a new unique <see cref="SequenceQueryId"/>.
    /// </summary>
    /// <returns>A new <see cref="SequenceQueryId"/>.</returns>
    public static SequenceQueryId New() => new(Guid.NewGuid().ToString());
}
