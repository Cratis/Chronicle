// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Security;

/// <summary>
/// Represents an authorization type.
/// </summary>
/// <param name="Value">The underlying value.</param>
public record AuthorizationType(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Represents an unset <see cref="AuthorizationType"/>.
    /// </summary>
    public static readonly AuthorizationType NotSet = new(string.Empty);

    /// <summary>
    /// Implicitly converts from <see cref="string"/> to <see cref="AuthorizationType"/>.
    /// </summary>
    /// <param name="value">The <see cref="string"/> to convert.</param>
    /// <returns>The converted <see cref="AuthorizationType"/>.</returns>
    public static implicit operator AuthorizationType(string value) => new(value);
}
