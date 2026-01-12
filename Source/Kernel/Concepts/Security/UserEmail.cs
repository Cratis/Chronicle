// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Security;

/// <summary>
/// Represents a user email address.
/// </summary>
/// <param name="Value">The underlying value.</param>
public record UserEmail(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Implicitly converts from <see cref="string"/> to <see cref="UserEmail"/>.
    /// </summary>
    /// <param name="value">The <see cref="string"/> to convert.</param>
    /// <returns>The converted <see cref="UserEmail"/>.</returns>
    public static implicit operator UserEmail(string value) => new(value);
}
