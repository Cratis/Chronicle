// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Generations.Enablement;

namespace Cratis.Chronicle.Storage.Generations;

/// <summary>Stores a per-store monotonic compatibility floor and atomic audit trail.</summary>
public interface IGenerationFloorStorage
{
    /// <summary>Reads the floor without creating storage.</summary>
    /// <returns>The floor, or null if never advanced.</returns>
    Task<GenerationFloor?> Get();

    /// <summary>Conditionally advances the floor and its audit trail together.</summary>
    /// <param name="advance">The explicit request.</param>
    /// <returns>The conditional outcome.</returns>
    Task<GenerationFloorAdvanceResult> Advance(GenerationFloorAdvance advance);

    /// <summary>Reads committed floor revisions without creating storage.</summary>
    /// <returns>The ordered audit trail.</returns>
    Task<IReadOnlyList<GenerationFloor>> GetHistory();
}
