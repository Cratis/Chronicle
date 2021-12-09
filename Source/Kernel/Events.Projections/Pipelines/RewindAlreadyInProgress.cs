// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Events.Projections.Pipelines
{
    /// <summary>
    /// Exception that is thrown if one attempts to rewind while a rewind is already in progress.
    /// </summary>
    public class RewindAlreadyInProgress : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RewindAlreadyInProgress"/> class.
        /// </summary>
        /// <param name="pipeline"><see cref="IProjectionPipeline"/> already rewinding.</param>
        public RewindAlreadyInProgress(IProjectionPipeline pipeline) : base($"Projection '{pipeline.Projection.Name}' with identifier '{pipeline.Projection.Identifier}' is already rewinding.")
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RewindAlreadyInProgress"/> class.
        /// </summary>
        /// <param name="pipeline"><see cref="IProjectionPipeline"/> already rewinding.</param>
        /// <param name="configurationId"><see cref="ProjectionResultStoreConfigurationId"/> that is rewinding.</param>
        public RewindAlreadyInProgress(
            IProjectionPipeline pipeline,
            ProjectionResultStoreConfigurationId configurationId) : base($"Projection '{pipeline.Projection.Name}' with identifier '{pipeline.Projection.Identifier}' is already rewinding for result store with configuration identifier '{configurationId}'.")
        {
        }
    }
}
