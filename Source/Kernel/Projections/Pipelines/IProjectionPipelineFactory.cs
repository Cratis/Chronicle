// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using EngineProjection = Cratis.Chronicle.Projections.IProjection;

namespace Cratis.Chronicle.Projections.Pipelines;

/// <summary>
/// Defines a system for working with <see cref="IProjectionPipeline">projection pipelines</see>.
/// </summary>
public interface IProjectionPipelineFactory
{
    /// <summary>
    /// Create a projection pipeline for a given <see cref="EngineProjection"/>.
    /// </summary>
    /// <param name="eventStore">The <see cref="EventStoreName"/> the pipeline is for.</param>
    /// <param name="namespace">The <see cref="EventStoreNamespaceName"/> the pipeline is for.</param>
    /// <param name="projection"><see cref="EngineProjection"/> the pipeline is for.</param>
    /// <param name="definition">The <see cref="ProjectionDefinition"/> to register.</param>
    /// <returns>The <see cref="IProjectionPipeline"/> instance.</returns>
    IProjectionPipeline Create(EventStoreName eventStore, EventStoreNamespaceName @namespace, EngineProjection projection, ProjectionDefinition definition);
}
