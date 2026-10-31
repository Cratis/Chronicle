// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Generations.Bindings;

namespace Cratis.Chronicle.Storage.MongoDB.Generations.Fencing;

/// <summary>
/// Represents the right of one owner to write one generation of one materialization.
/// </summary>
/// <param name="MaterializationKey">The key identifying the materialization the gate guards.</param>
/// <param name="Generation">The generation of the materialization the gate guards.</param>
/// <param name="Epoch">The owner epoch the gate record held when the right was granted.</param>
public record FenceToken(string MaterializationKey, ulong Generation, long Epoch)
{
    /// <summary>
    /// Gets the identifier of the gate record guarding the materialization generation.
    /// </summary>
    public string GateId => GenerationGateId.Format(MaterializationKey, Generation);
}
