// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Projections.Definitions;

namespace Aksio.Cratis.Kernel.Grains.Projections;

/// <summary>
/// Defines a system that is responsible for supervises projections in the system.
/// </summary>
public interface IProjections : IGrainWithIntegerKey
{
    /// <summary>
    /// Rehydrate all projections for all microservices and tenants.
    /// </summary>
    /// <returns>Async task.</returns>
    Task Rehydrate();

    /// <summary>
    /// Register a <see cref="ProjectionDefinition"/> with a <see cref="ProjectionPipelineDefinition"/>.
    /// </summary>
    /// <param name="microserviceId"><see cref="MicroserviceId"/> to register for.</param>
    /// <param name="registrations">A collection of <see cref="ProjectionAndPipeline"/>.</param>
    /// <returns>Async task.</returns>
    /// <remarks>
    /// If any of the projections are already in the system, it will look for changes in the definition
    /// and possibly rewind the projection.
    /// </remarks>
    Task Register(MicroserviceId microserviceId, IEnumerable<ProjectionAndPipeline> registrations);
}
