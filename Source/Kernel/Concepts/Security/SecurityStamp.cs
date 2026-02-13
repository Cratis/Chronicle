// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Security;

/// <summary>
/// Represents a security stamp.
/// </summary>
/// <param name="Value">The underlying value.</param>
public record SecurityStamp(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Implicitly converts from <see cref="string"/> to <see cref="SecurityStamp"/>.
    /// </summary>
    /// <param name="value">The <see cref="string"/> to convert.</param>
    /// <returns>The converted <see cref="SecurityStamp"/>.</returns>
    public static implicit operator SecurityStamp(string value) => new(value);
}
