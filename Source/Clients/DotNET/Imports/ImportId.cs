// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Imports;

/// <summary>
/// Represents the identity of an import session.
/// </summary>
/// <param name="Value">The underlying value.</param>
public record ImportId(Guid Value) : ConceptAs<Guid>(Value)
{
    /// <summary>
    /// Represents an unset <see cref="ImportId"/>.
    /// </summary>
    public static readonly ImportId NotSet = new(Guid.Empty);

    /// <summary>
    /// Implicitly converts from <see cref="Guid"/> to <see cref="ImportId"/>.
    /// </summary>
    /// <param name="value">The <see cref="Guid"/> to convert.</param>
    public static implicit operator ImportId(Guid value) => new(value);

    /// <summary>
    /// Creates a new <see cref="ImportId"/>.
    /// </summary>
    /// <returns>A new unique <see cref="ImportId"/>.</returns>
    public static ImportId New() => new(Guid.NewGuid());
}
