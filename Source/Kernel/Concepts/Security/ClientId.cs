// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Security;

/// <summary>
/// Represents a client identifier.
/// </summary>
/// <param name="Value">The underlying value.</param>
public record ClientId(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Gets the representation of an empty <see cref="ClientId"/>.
    /// </summary>
    public static readonly ClientId Empty = new(string.Empty);

    /// <summary>
    /// Implicitly converts from <see cref="string"/> to <see cref="ClientId"/>.
    /// </summary>
    /// <param name="value">The <see cref="string"/> to convert.</param>
    /// <returns>The converted <see cref="ClientId"/>.</returns>
    public static implicit operator ClientId(string value) => new(value);
}
