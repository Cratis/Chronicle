// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Concepts;

namespace Aksio.Cratis.Serialization;

/// <summary>
/// The unique identifier identifying a derived type.
/// </summary>
/// <param name="id">The string representation of a <see cref="Guid"/> that identifies the type uniquely.</param>
public record DerivedTypeId(Guid id) : ConceptAs<Guid>(id)
{
    /// <summary>
    /// Implicitly convert from <see cref="Guid"/> to <see cref="DerivedTypeId"/>.
    /// </summary>
    /// <param name="id"><see cref="Guid"/> to convert from.</param>
    public static implicit operator DerivedTypeId(Guid id) => new(id);

    /// <summary>
    /// Implicitly convert from a string representation of a <see cref="Guid"/> to <see cref="DerivedTypeId"/>.
    /// </summary>
    /// <param name="id">String <see cref="Guid"/> to convert from.</param>
    public static implicit operator DerivedTypeId(string id) => new(Guid.Parse(id));
}
