// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Security;

/// <summary>
/// Represents a username.
/// </summary>
/// <param name="Value">The underlying value.</param>
public record Username(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Gets the representation of an empty <see cref="Username"/>.
    /// </summary>
    public static readonly Username Empty = new(string.Empty);

    /// <summary>
    /// Implicitly converts from <see cref="string"/> to <see cref="Username"/>.
    /// </summary>
    /// <param name="value">The <see cref="string"/> to convert.</param>
    /// <returns>The converted <see cref="Username"/>.</returns>
    public static implicit operator Username(string value) => new(value);
}
