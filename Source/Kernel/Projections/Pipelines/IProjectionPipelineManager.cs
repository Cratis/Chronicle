// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Projections;
using EngineProjection = Cratis.Chronicle.Projections.IProjection;

namespace Cratis.Chronicle.Projections.Pipelines;

/// <summary>
/// Defines a system for working with <see cref="IProjectionPipeline">projection pipelines</see>.
/// </summary>
public interface IProjectionPipelineManager
{
    /// <summary>
    /// Create a projection pipeline for a given <see cref="EngineProjection"/>.
    /// </summary>
    /// <param name="eventStore">The <see cref="EventStoreName"/> the pipeline is for.</param>
    /// <param name="namespace">The <see cref="EventStoreNamespaceName"/> the pipeline is for.</param>
    /// <param name="projection"><see cref="EngineProjection"/> the pipeline is for.</param>
    /// <returns>The <see cref="IProjectionPipeline"/> instance.</returns>
    IProjectionPipeline GetFor(EventStoreName eventStore, EventStoreNamespaceName @namespace, EngineProjection projection);

    /// <summary>
    /// Evict any projection pipeline for a specific projection identifier.
    /// </summary>
    /// <param name="eventStore"><see cref="EventStoreName"/> the projection is for.</param>
    /// <param name="namespace">The <see cref="EventStoreNamespaceName"/> the projection is for.</param>
    /// <param name="id"><see cref="ProjectionId"/> of the projection to evict.</param>
    void EvictFor(EventStoreName eventStore, EventStoreNamespaceName @namespace, ProjectionId id);
}
