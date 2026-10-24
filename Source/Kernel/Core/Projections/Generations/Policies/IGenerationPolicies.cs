// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Generations;

namespace Cratis.Chronicle.Projections.Generations.Policies;

/// <summary>
/// Resolves generation policies without performing promotion or rollback.
/// </summary>
public interface IGenerationPolicies
{
    /// <summary>
    /// Resolves server, event store and projection policy, followed by a stricter-only client gate.
    /// </summary>
    /// <param name="eventStore">The event store.</param>
    /// <param name="projectionId">The projection.</param>
    /// <param name="isKernelOwned">Whether the kernel owns the projection; these are never gated or retained for rollback.</param>
    /// <param name="isReducer">Whether the projection is a reducer; reducer rollback is currently disabled.</param>
    /// <param name="requestedGate">An optional client-requested gate.</param>
    /// <returns>The effective policy, or an inert policy when projection generations are disabled.</returns>
    GenerationPromotionPolicy Resolve(EventStoreName eventStore, ProjectionId projectionId, bool isKernelOwned, bool isReducer, PromotionGateMode? requestedGate = null);
}
