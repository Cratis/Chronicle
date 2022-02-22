// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Aksio.Cratis.Events.Projections.Pipelines.JobSteps
{
    /// <summary>
    /// Represents a <see cref="IProjectionPipelineJobStep"/> for rewinding.
    /// </summary>
    public class Rewind : IProjectionPipelineJobStep
    {
        readonly IProjectionPipeline _pipeline;
        readonly IProjectionPositions _projectionPositions;
        readonly ILogger<Rewind> _logger;
        IProjectionSinkRewindScope? _rewindScope;

        /// <inheritdoc/>
        public string Name => "Rewind";

        /// <summary>
        /// Gets the <see cref="ProjectionSinkConfigurationId"/> for the rewind.
        /// </summary>
        public ProjectionSinkConfigurationId ConfigurationId { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Rewind"/> class.
        /// </summary>
        /// <param name="pipeline"><see cref="IProjectionPipeline"/> the rewind is for.</param>
        /// <param name="projectionPositions"><see cref="IProjectionPositions"/> for working with the positions of the projections.</param>
        /// <param name="configurationId"><see cref="ProjectionSinkConfigurationId"/> to rewind.</param>
        /// <param name="logger">For logging.</param>
        public Rewind(
            IProjectionPipeline pipeline,
            IProjectionPositions projectionPositions,
            ProjectionSinkConfigurationId configurationId,
            ILogger<Rewind> logger)
        {
            _pipeline = pipeline;
            _projectionPositions = projectionPositions;
            ConfigurationId = configurationId;
            _logger = logger;
        }

        /// <inheritdoc/>
        public async Task Perform(ProjectionPipelineJobStatus jobStatus)
        {
            _logger.Rewinding(_pipeline.Projection.Identifier, ConfigurationId);
            var sink = _pipeline.Sinks[ConfigurationId];
            _rewindScope = sink.BeginRewind();
            jobStatus.ReportTask($"Resetting positions for '{sink.Name}' with configuration id {ConfigurationId}");
            await _projectionPositions.Reset(_pipeline.Projection, ConfigurationId);
        }

        /// <inheritdoc/>
        public Task PerformPostJob(ProjectionPipelineJobStatus jobStatus)
        {
            var sink = _pipeline.Sinks[ConfigurationId];
            jobStatus.ReportTask($"Ending rewind scope '{sink.Name}' with configuration id {ConfigurationId}");
            _rewindScope?.Dispose();
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task Stop() => Task.CompletedTask;
    }
}
