// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Projections.Generations;

/// <summary>
/// Represents the lifecycle state of a <see cref="MaterializationGeneration"/>, as defined by ADR 0014.
/// </summary>
public enum MaterializationGenerationState
{
    /// <summary>
    /// Isolated historical construction, possibly unstarted or waiting. Never served by default.
    /// </summary>
    Building = 0,

    /// <summary>
    /// Historical coverage is proven and the generation follows live events. Not served by default.
    /// </summary>
    Following = 1,

    /// <summary>
    /// The generation selected by the authoritative binding; follows live events.
    /// </summary>
    Active = 2,

    /// <summary>
    /// Displaced from serving, still following, and a bounded rollback target.
    /// </summary>
    Retired = 3,

    /// <summary>
    /// A non-following retained snapshot.
    /// </summary>
    Archived = 4,

    /// <summary>
    /// An unserved canceled, superseded or terminally failed build, drained before cleanup.
    /// </summary>
    Abandoned = 5,

    /// <summary>
    /// Physical data removed; only identity and audit metadata remain.
    /// </summary>
    Removed = 6
}
