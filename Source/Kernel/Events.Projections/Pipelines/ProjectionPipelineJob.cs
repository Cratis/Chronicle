// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Events.Projections.Pipelines
{
    /// <summary>
    /// Represents an implementation of <see cref="IProjectionPipelineJob"/>.
    /// </summary>
    public class ProjectionPipelineJob : IProjectionPipelineJob
    {
        public ProjectionPipelineJob(string name, IEnumerable<IProjectionPipelineJobStep> steps)
        {
            Name = name;
            Steps = steps;
        }

        /// <inheritdoc/>
        public string Name { get; }

        /// <inheritdoc/>
        public PipelineJobStatus Status { get; } = new();

        /// <inheritdoc/>
        public IEnumerable<IProjectionPipelineJobStep> Steps { get; }

        /// <inheritdoc/>
        public async Task Run()
        {
            foreach (var step in Steps)
            {
                await step.Perform(Status);
            }

            foreach (var step in Steps)
            {
                await step.PerformPostJob(Status);
            }
        }
    }
}
