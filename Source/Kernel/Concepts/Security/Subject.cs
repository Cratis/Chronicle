// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Security;

/// <summary>
/// Represents a subject identifier.
/// </summary>
/// <param name="Value">The underlying value.</param>
public record Subject(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Represents an unset <see cref="Subject"/>.
    /// </summary>
    public static readonly Subject NotSet = new(string.Empty);

    /// <summary>
    /// Implicitly converts from <see cref="string"/> to <see cref="Subject"/>.
    /// </summary>
    /// <param name="value">The <see cref="string"/> to convert.</param>
    /// <returns>The converted <see cref="Subject"/>.</returns>
    public static implicit operator Subject(string value) => new(value);
}
