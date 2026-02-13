// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Security;

/// <summary>
/// Represents an authentication token.
/// </summary>
/// <param name="Value">The actual value.</param>
public record Token(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Gets the representation of an empty <see cref="Token"/>.
    /// </summary>
    public static readonly Token Empty = new(string.Empty);

    /// <summary>
    /// Implicitly convert from a string to <see cref="Token"/>.
    /// </summary>
    /// <param name="value">String to convert from.</param>
    public static implicit operator Token(string value) => new(value);
}
