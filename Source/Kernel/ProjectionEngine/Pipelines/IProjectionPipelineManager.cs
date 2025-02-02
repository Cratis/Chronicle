// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using EngineProjection = Cratis.Chronicle.ProjectionEngine.IProjection;

namespace Cratis.Chronicle.ProjectionEngine.Pipelines;

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
}
