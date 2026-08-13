// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.SequenceQueries;

/// <summary>
/// Represents the identity that owns a saved event sequence query.
/// </summary>
/// <param name="Value">The underlying value.</param>
/// <remarks>
/// A query shared with everyone still records who created it, so the workbench can show provenance
/// and only offer deletion to its owner.
/// </remarks>
public record SequenceQueryOwner(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Represents an unset <see cref="SequenceQueryOwner"/>.
    /// </summary>
    public static readonly SequenceQueryOwner NotSet = new(string.Empty);

    /// <summary>
    /// Implicitly convert from <see cref="string"/> to <see cref="SequenceQueryOwner"/>.
    /// </summary>
    /// <param name="value">Value to convert from.</param>
    public static implicit operator SequenceQueryOwner(string value) => new(value);
}
