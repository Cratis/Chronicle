// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// A strongly-typed contact identifier value, backed by a <see cref="Guid"/> concept. Used to verify a
/// child collection keyed by a <see cref="ConceptAs{T}"/> over a <see cref="Guid"/> materializes alongside
/// a <see cref="DateOnly"/>-keyed child collection on the same read model.
/// </summary>
/// <param name="Value">The underlying value.</param>
public record ContactId(Guid Value) : ConceptAs<Guid>(Value)
{
    /// <summary>
    /// Implicitly converts a <see cref="Guid"/> to a <see cref="ContactId"/>.
    /// </summary>
    /// <param name="value">The <see cref="Guid"/> to convert.</param>
    public static implicit operator ContactId(Guid value) => new(value);
}
