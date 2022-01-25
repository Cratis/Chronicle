// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Events.Projections.Pipelines
{
    /// <summary>
    /// Represents an implementation of <see cref="IProjectionPipelineJob"/>.
    /// </summary>
    public class ProjectionPipelineJob : IProjectionPipelineJob
    {
        /// <inheritdoc/>
        public string Name { get; }

        /// <inheritdoc/>
        public ProjectionPipelineJobStatus Status { get; } = new();

        /// <inheritdoc/>
        public IEnumerable<IProjectionPipelineJobStep> Steps { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProjectionPipelineJob"/> class.
        /// </summary>
        /// <param name="name">Name of the job.</param>
        /// <param name="steps">Collection of <see cref="IProjectionPipelineJobStep"/>.</param>
        public ProjectionPipelineJob(string name, IEnumerable<IProjectionPipelineJobStep> steps)
        {
            Name = name;
            Steps = steps;
        }

        /// <inheritdoc/>
        public async Task Run()
        {
            foreach (var step in Steps)
            {
                Status.ReportStep(step);
                await step.Perform(Status);
                Status.ReportProgress(1.0f);
            }

            foreach (var step in Steps)
            {
                await step.PerformPostJob(Status);
            }
        }

        /// <inheritdoc/>
        public async Task Stop()
        {
            foreach (var step in Steps)
            {
                await step.Stop();
            }

            Status.ReportStopped();
        }
    }
}
