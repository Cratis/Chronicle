// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Patterns;

/// <summary>
/// Represents the canonical, stable identity of a <see cref="FacetSet"/>.
/// </summary>
/// <param name="Value">The actual value.</param>
/// <remarks>
/// The key is what a mined pattern is counted, stored and looked up by, so it has to mean the same thing on every
/// silo and across restarts. It is built by <see cref="FacetSet"/> from ordered, escaped facets rather than from a
/// hash - two sets are the same pattern exactly when they read the same, and the key stays legible in storage.
/// </remarks>
public record FacetSetKey(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Represents the key of the empty <see cref="FacetSet"/>.
    /// </summary>
    public static readonly FacetSetKey Empty = new(string.Empty);

    /// <summary>
    /// Implicitly convert from a string to <see cref="FacetSetKey"/>.
    /// </summary>
    /// <param name="key">String to convert from.</param>
    public static implicit operator FacetSetKey(string key) => new(key);
}
