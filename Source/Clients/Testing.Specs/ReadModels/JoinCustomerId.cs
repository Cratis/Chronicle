// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// The identifier of a customer that an order joins against for enrichment.
/// </summary>
/// <param name="Value">The underlying value.</param>
public record JoinCustomerId(Guid Value) : ConceptAs<Guid>(Value)
{
    /// <summary>
    /// Implicitly converts a <see cref="Guid"/> to a <see cref="JoinCustomerId"/>.
    /// </summary>
    /// <param name="value">The <see cref="Guid"/> to convert.</param>
    public static implicit operator JoinCustomerId(Guid value) => new(value);
}
