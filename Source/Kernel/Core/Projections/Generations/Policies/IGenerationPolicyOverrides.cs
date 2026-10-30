// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Projections;

namespace Cratis.Chronicle.Projections.Generations.Policies;

/// <summary>
/// Provides optional event store and projection policy overrides, independently of their storage.
/// </summary>
public interface IGenerationPolicyOverrides
{
    /// <summary>
    /// Gets policy overrides for an event store.
    /// </summary>
    /// <param name="eventStore">The event store.</param>
    /// <returns>The overrides, or null if none are configured.</returns>
    GenerationPromotionPolicyOverride? GetForEventStore(EventStoreName eventStore);

    /// <summary>
    /// Gets policy overrides for a projection within an event store.
    /// </summary>
    /// <param name="eventStore">The event store.</param>
    /// <param name="projectionId">The projection.</param>
    /// <returns>The overrides, or null if none are configured.</returns>
    GenerationPromotionPolicyOverride? GetForProjection(EventStoreName eventStore, ProjectionId projectionId);
}
