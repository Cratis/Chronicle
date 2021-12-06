// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Events.Projections.Pipelines
{
    /// <summary>
    /// Defines a step in a <see cref="IProjectionPipelineJob"/>.
    /// </summary>
    public interface IProjectionPipelineJobStep
    {
        /// <summary>
        /// Gets the name of the step.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Perform the step.
        /// </summary>
        /// <param name="jobStatus">The <see cref="PipelineJobStatus"/>.</param>
        /// <returns>Async task for continuation.</returns>
        Task Perform(PipelineJobStatus jobStatus);

        /// <summary>
        /// Perform any post job operations.
        /// </summary>
        /// <param name="jobStatus">The <see cref="PipelineJobStatus"/>.</param>
        /// <returns>Async task for continuation.</returns>
        Task PerformPostJob(PipelineJobStatus jobStatus);
    }
}
