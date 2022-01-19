// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events.Projections.Definitions;
using Aksio.Cratis.Events.Projections.Pipelines;

#pragma warning disable CA1721 // Pipelines property is confusing since there is a GetPipelines - they do differ - see GH issue #103.

namespace Aksio.Cratis.Events.Projections
{
    /// <summary>
    /// Defines a system that is responsible for supervises projections in the system.
    /// </summary>
    public interface IProjections
    {
        /// <summary>
        /// Register a <see cref="ProjectionDefinition"/> with a <see cref="ProjectionPipelineDefinition"/>.
        /// </summary>
        /// <param name="projectionDefinition"><see cref="ProjectionDefinition"/> to register.</param>
        /// <param name="pipelineDefinition">The <see cref="ProjectionPipelineDefinition"/> for the projection.</param>
        /// <returns>Async task.</returns>
        /// <remarks>
        /// If the projection is already in the system, the supervisor will see if there are any differences
        /// and possible set up the projection for rewind.
        /// </remarks>
        Task Register(ProjectionDefinition projectionDefinition, ProjectionPipelineDefinition pipelineDefinition);

        /// <summary>
        /// Get all <see cref="IProjectionPipeline">projection pipelines</see> in the system.
        /// </summary>
        /// <returns>Collection of <see cref="IProjectionPipeline"/>.</returns>
        IEnumerable<IProjectionPipeline> GetPipelines();

        /// <summary>
        /// Gets an observable of all <see cref="IProjectionPipeline"/> in the system.
        /// </summary>
        IObservable<IProjectionPipeline> Pipelines { get; }

        /// <summary>
        /// Get a specific <see cref="IProjectionPipeline"/> by the identifier.
        /// </summary>
        /// <param name="id"><see cref="ProjectionId"/> to get.</param>
        /// <returns>The <see cref="IProjectionPipeline"/>.</returns>
        IProjectionPipeline GetById(ProjectionId id);

        /// <summary>
        /// Start the supervisor.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task Start();
    }
}
