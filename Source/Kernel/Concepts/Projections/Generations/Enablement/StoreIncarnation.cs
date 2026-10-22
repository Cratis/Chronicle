// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Projections.Generations.Enablement;

/// <summary>The ownership incarnation of an event store.</summary>
/// <param name="Value">The incarnation identity.</param>
public record StoreIncarnation(Guid Value) : ConceptAs<Guid>(Value)
{
    /// <summary>An unspecified incarnation.</summary>
    public static readonly StoreIncarnation Unspecified = new(Guid.Empty);
}
