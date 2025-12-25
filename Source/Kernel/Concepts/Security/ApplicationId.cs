// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Security;

/// <summary>
/// Represents an application identifier.
/// </summary>
/// <param name="Value">The underlying value.</param>
public record ApplicationId(Guid Value) : ConceptAs<Guid>(Value)
{
    /// <summary>
    /// Represents an unset <see cref="ApplicationId"/>.
    /// </summary>
    public static readonly ApplicationId NotSet = new(Guid.Empty);

    /// <summary>
    /// Implicitly converts from <see cref="Guid"/> to <see cref="ApplicationId"/>.
    /// </summary>
    /// <param name="value">The <see cref="Guid"/> to convert.</param>
    /// <returns>The converted <see cref="ApplicationId"/>.</returns>
    public static implicit operator ApplicationId(Guid value) => new(value);
}
