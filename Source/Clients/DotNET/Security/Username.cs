// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Security;

/// <summary>
/// Represents a username.
/// </summary>
/// <param name="Value">The actual value.</param>
public record Username(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Gets the representation of an empty <see cref="Username"/>.
    /// </summary>
    public static readonly Username Empty = new(string.Empty);

    /// <summary>
    /// Implicitly convert from a string to <see cref="Username"/>.
    /// </summary>
    /// <param name="value">String to convert from.</param>
    public static implicit operator Username(string value) => new(value);
}
