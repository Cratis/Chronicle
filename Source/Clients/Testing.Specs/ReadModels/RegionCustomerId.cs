// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// The identifier of a customer that a region-carrying order joins against for enrichment.
/// </summary>
/// <param name="Value">The underlying value.</param>
public record RegionCustomerId(Guid Value) : ConceptAs<Guid>(Value)
{
    /// <summary>
    /// Implicitly converts a <see cref="Guid"/> to a <see cref="RegionCustomerId"/>.
    /// </summary>
    /// <param name="value">The <see cref="Guid"/> to convert.</param>
    public static implicit operator RegionCustomerId(Guid value) => new(value);
}
