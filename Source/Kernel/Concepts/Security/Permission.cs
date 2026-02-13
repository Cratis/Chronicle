// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Security;

/// <summary>
/// Represents a permission.
/// </summary>
/// <param name="Value">The underlying value.</param>
public record Permission(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Implicitly converts from <see cref="string"/> to <see cref="Permission"/>.
    /// </summary>
    /// <param name="value">The <see cref="string"/> to convert.</param>
    /// <returns>The converted <see cref="Permission"/>.</returns>
    public static implicit operator Permission(string value) => new(value);
}
