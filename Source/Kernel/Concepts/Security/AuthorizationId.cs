// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Security;

/// <summary>
/// Represents an authorization identifier.
/// </summary>
/// <param name="Value">The underlying value.</param>
public record AuthorizationId(Guid Value) : ConceptAs<Guid>(Value)
{
    /// <summary>
    /// Represents an unset <see cref="AuthorizationId"/>.
    /// </summary>
    public static readonly AuthorizationId NotSet = new(Guid.Empty);

    /// <summary>
    /// Implicitly converts from <see cref="Guid"/> to <see cref="AuthorizationId"/>.
    /// </summary>
    /// <param name="value">The <see cref="Guid"/> to convert.</param>
    /// <returns>The converted <see cref="AuthorizationId"/>.</returns>
    public static implicit operator AuthorizationId(Guid value) => new(value);
}
