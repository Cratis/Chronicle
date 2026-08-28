// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Patterns;

/// <summary>
/// Represents the value a facet holds for a single event or pattern.
/// </summary>
/// <param name="Value">The actual value.</param>
public record FacetValue(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Represents an unspecified <see cref="FacetValue"/>.
    /// </summary>
    /// <remarks>
    /// A facet the event carries nothing for is unspecified rather than null - a fact that is absent should never
    /// take part in a mined pattern, and a sentinel keeps every consumer from having to null-check its way there.
    /// </remarks>
    public static readonly FacetValue Unspecified = new(string.Empty);

    /// <summary>
    /// Gets a value indicating whether the facet holds a value.
    /// </summary>
    public bool IsSpecified => !string.IsNullOrEmpty(Value);

    /// <summary>
    /// Implicitly convert from a string to <see cref="FacetValue"/>.
    /// </summary>
    /// <param name="value">String to convert from.</param>
    public static implicit operator FacetValue(string? value) => value is null ? Unspecified : new(value);
}
