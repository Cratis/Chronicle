// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections.Definitions;

namespace Cratis.Chronicle.Grains.Projections;

/// <summary>
/// Defines a system that is responsible for supervises projections in the system.
/// </summary>
public interface IProjections : IGrainWithStringKey
{
    /// <summary>
    /// Register a <see cref="ProjectionDefinition"/> with a <see cref="ProjectionPipelineDefinition"/> for the event store it belongs to.
    /// </summary>
    /// <param name="registrations">A collection of <see cref="ProjectionAndPipeline"/>.</param>
    /// <returns>Async task.</returns>
    /// <remarks>
    /// If any of the projections are already in the system, it will look for changes in the definition
    /// and possibly rewind the projection.
    /// </remarks>
    Task Register(IEnumerable<ProjectionAndPipeline> registrations);
}
