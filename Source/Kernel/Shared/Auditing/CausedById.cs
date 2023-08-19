// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Auditing;

/// <summary>
/// Represents an identifier for details holding actual <see cref="CausedBy"/>.
/// </summary>
/// <param name="Value">The actual value.</param>
public record CausedById(Guid Value) : ConceptAs<Guid>(Value)
{
    /// <summary>
    /// Implicitly convert from <see cref="Guid"/> to <see cref="CausedById"/>.
    /// </summary>
    /// <param name="value">Guid to convert from.</param>
    public static implicit operator CausedById(Guid value) => new(value);
}
