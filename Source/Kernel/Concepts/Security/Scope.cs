// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Security;

/// <summary>
/// Represents a scope.
/// </summary>
/// <param name="Value">The underlying value.</param>
public record Scope(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Represents an unset <see cref="Scope"/>.
    /// </summary>
    public static readonly Scope NotSet = new(string.Empty);

    /// <summary>
    /// Implicitly converts from <see cref="string"/> to <see cref="Scope"/>.
    /// </summary>
    /// <param name="value">The <see cref="string"/> to convert.</param>
    /// <returns>The converted <see cref="Scope"/>.</returns>
    public static implicit operator Scope(string value) => new(value);
}
