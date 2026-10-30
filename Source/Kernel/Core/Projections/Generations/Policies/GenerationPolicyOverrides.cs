// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Projections;

namespace Cratis.Chronicle.Projections.Generations.Policies;

/// <summary>
/// Provides no overrides until event store and projection policy storage is introduced.
/// Discovered through the kernel's interface binding convention.
/// </summary>
public class GenerationPolicyOverrides : IGenerationPolicyOverrides
{
    /// <inheritdoc/>
    public GenerationPromotionPolicyOverride? GetForEventStore(EventStoreName eventStore) => null;

    /// <inheritdoc/>
    public GenerationPromotionPolicyOverride? GetForProjection(EventStoreName eventStore, ProjectionId projectionId) => null;
}
