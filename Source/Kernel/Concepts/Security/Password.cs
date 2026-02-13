// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Security;

/// <summary>
/// Represents a password.
/// </summary>
/// <param name="Value">The underlying value.</param>
public record Password(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Gets the representation of an empty <see cref="Password"/>.
    /// </summary>
    public static readonly Password Empty = new(string.Empty);

    /// <summary>
    /// Implicitly converts from <see cref="string"/> to <see cref="Password"/>.
    /// </summary>
    /// <param name="value">The <see cref="string"/> to convert.</param>
    /// <returns>The converted <see cref="Password"/>.</returns>
    public static implicit operator Password(string value) => new(value);
}
