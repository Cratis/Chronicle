// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.Generations.Fencing;

/// <summary>
/// Represents the identifier of one fenced batch attempt, written as its receipt.
/// </summary>
/// <param name="Value">The underlying value.</param>
public record FenceBatchId(Guid Value) : ConceptAs<Guid>(Value)
{
    /// <summary>
    /// Creates a new unique <see cref="FenceBatchId"/>.
    /// </summary>
    /// <returns>A new <see cref="FenceBatchId"/>.</returns>
    public static FenceBatchId New() => new(Guid.NewGuid());
}
