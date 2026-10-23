// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Projections.Generations.Enablement;

namespace Cratis.Chronicle.Generations.Admission;

/// <summary>The explicit, membership-checked advancement boundary; no runtime caller is installed.</summary>
public interface IGenerationFloorAdvancement
{
    /// <summary>Advances one event store only after fresh uniform-membership evidence.</summary>
    /// <param name="eventStore">The event store.</param>
    /// <param name="advance">The explicit request.</param>
    /// <returns>The conditional outcome.</returns>
    Task<GenerationFloorAdvanceResult> Advance(EventStoreName eventStore, GenerationFloorAdvance advance);
}
