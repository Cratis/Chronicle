// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Projections.Definitions;

namespace Cratis.Kernel.Grains.Projections;

/// <summary>
/// Defines a system that is responsible for supervises projections in the system.
/// </summary>
public interface IProjections : IGrainWithIntegerKey
{
    /// <summary>
    /// Rehydrate all projections for all event stores and namespaces.
    /// </summary>
    /// <returns>Async task.</returns>
    Task Rehydrate();

    /// <summary>
    /// Register a <see cref="ProjectionDefinition"/> with a <see cref="ProjectionPipelineDefinition"/>.
    /// </summary>
    /// <param name="eventStore"><see cref="EventStoreName"/> to register for.</param>
    /// <param name="registrations">A collection of <see cref="ProjectionAndPipeline"/>.</param>
    /// <returns>Async task.</returns>
    /// <remarks>
    /// If any of the projections are already in the system, it will look for changes in the definition
    /// and possibly rewind the projection.
    /// </remarks>
    Task Register(EventStoreName eventStore, IEnumerable<ProjectionAndPipeline> registrations);
}
