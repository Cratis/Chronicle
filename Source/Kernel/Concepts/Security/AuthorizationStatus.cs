// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Security;

/// <summary>
/// Represents an authorization status.
/// </summary>
/// <param name="Value">The underlying value.</param>
public record AuthorizationStatus(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Represents an unset <see cref="AuthorizationStatus"/>.
    /// </summary>
    public static readonly AuthorizationStatus NotSet = new(string.Empty);

    /// <summary>
    /// Implicitly converts from <see cref="string"/> to <see cref="AuthorizationStatus"/>.
    /// </summary>
    /// <param name="value">The <see cref="string"/> to convert.</param>
    /// <returns>The converted <see cref="AuthorizationStatus"/>.</returns>
    public static implicit operator AuthorizationStatus(string value) => new(value);
}
