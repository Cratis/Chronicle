// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Patterns;

/// <summary>
/// Represents the scope patterns are mined and looked up within - typically the identity of the user the behavior
/// belongs to.
/// </summary>
/// <param name="Value">The actual value.</param>
public record PatternGroupingKey(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Represents an unspecified <see cref="PatternGroupingKey"/>.
    /// </summary>
    public static readonly PatternGroupingKey Unspecified = new(string.Empty);

    /// <summary>
    /// Gets a value indicating whether the key identifies a scope.
    /// </summary>
    public bool IsSpecified => !string.IsNullOrEmpty(Value);

    /// <summary>
    /// Implicitly convert from a string to <see cref="PatternGroupingKey"/>.
    /// </summary>
    /// <param name="key">String to convert from.</param>
    public static implicit operator PatternGroupingKey(string? key) => key is null ? Unspecified : new(key);
}
