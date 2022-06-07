// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events.Projections.Definitions;
using Orleans;

namespace Aksio.Cratis.Events.Projections.Grains;

/// <summary>
/// Defines a system that is responsible for supervises projections in the system.
/// </summary>
public interface IProjections : IGrainWithGuidKey
{
    /// <summary>
    /// Rehydrate all projections for all microservices and tenants.
    /// </summary>
    /// <returns>Async task.</returns>
    Task Rehydrate();

    /// <summary>
    /// Register a <see cref="ProjectionDefinition"/> with a <see cref="ProjectionPipelineDefinition"/>.
    /// </summary>
    /// <param name="projectionDefinition"><see cref="ProjectionDefinition"/> to register.</param>
    /// <param name="pipelineDefinition">The <see cref="ProjectionPipelineDefinition"/> for the projection.</param>
    /// <returns>Async task.</returns>
    /// <remarks>
    /// If the projection is already in the system, it will look for changes in the definition differences
    /// and possibly rewind the projection.
    /// </remarks>
    Task Register(ProjectionDefinition projectionDefinition, ProjectionPipelineDefinition pipelineDefinition);
}
