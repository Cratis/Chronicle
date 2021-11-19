// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Events.Projections.Definitions;

namespace Cratis.Events.Projections
{
    /// <summary>
    /// Defines a system that is responsible for supervises projections in the system.
    /// </summary>
    public interface IProjectionsCoordinator
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
        /// Start the supervisor.
        /// </summary>
        void Start();
    }
}
