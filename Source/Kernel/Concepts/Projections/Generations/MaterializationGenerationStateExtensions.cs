// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Projections.Generations;

/// <summary>
/// Extension methods for <see cref="MaterializationGenerationState"/>.
/// </summary>
public static class MaterializationGenerationStateExtensions
{
    /// <summary>
    /// Check whether a generation may move from one state to another, as defined by the lifecycle in ADR 0014.
    /// </summary>
    /// <remarks>Expiry may move a Retired generation directly to Removed when retention is disabled, after draining and cleanup.</remarks>
    /// <param name="from">The current <see cref="MaterializationGenerationState"/>.</param>
    /// <param name="to">The <see cref="MaterializationGenerationState"/> to move to.</param>
    /// <returns>True if the transition is allowed, false if not.</returns>
    public static bool CanTransitionTo(this MaterializationGenerationState from, MaterializationGenerationState to) => (from, to) switch
    {
        (MaterializationGenerationState.Building, MaterializationGenerationState.Following) => true,
        (MaterializationGenerationState.Building, MaterializationGenerationState.Abandoned) => true,
        (MaterializationGenerationState.Following, MaterializationGenerationState.Active) => true,
        (MaterializationGenerationState.Following, MaterializationGenerationState.Abandoned) => true,
        (MaterializationGenerationState.Active, MaterializationGenerationState.Retired) => true,
        (MaterializationGenerationState.Retired, MaterializationGenerationState.Active) => true,
        (MaterializationGenerationState.Retired, MaterializationGenerationState.Archived) => true,
        (MaterializationGenerationState.Retired, MaterializationGenerationState.Removed) => true,
        (MaterializationGenerationState.Archived, MaterializationGenerationState.Removed) => true,
        (MaterializationGenerationState.Abandoned, MaterializationGenerationState.Removed) => true,
        _ => false
    };

    /// <summary>
    /// Check whether a generation in the given state handles live events.
    /// </summary>
    /// <param name="state">The <see cref="MaterializationGenerationState"/>.</param>
    /// <returns>True if the generation follows live events, false if not.</returns>
    public static bool IsFollowingLiveEvents(this MaterializationGenerationState state) =>
        state is MaterializationGenerationState.Following or MaterializationGenerationState.Active or MaterializationGenerationState.Retired;
}
