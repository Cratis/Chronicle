// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Events.Projections.Pipelines.JobSteps
{
    /// <summary>
    /// Represents a <see cref="IProjectionPipelineJobStep"/> for rewinding.
    /// </summary>
    public class Rewind : IProjectionPipelineJobStep
    {
        readonly IProjectionPipeline _pipeline;
        readonly IProjectionPositions _projectionPositions;
        readonly ProjectionResultStoreConfigurationId _configurationId;
        readonly ILogger<Rewind> _logger;
        IProjectionResultStoreRewindScope? _rewindScope;

        /// <summary>
        /// Initializes a new instance of the <see cref="Rewind"/> class.
        /// </summary>
        /// <param name="pipeline"><see cref="IProjectionPipeline"/> the rewind is for.</param>
        /// <param name="projectionPositions"><see cref="IProjectionPositions"/> for working with the positions of the projections.</param>
        /// <param name="configurationId"><see cref="ProjectionResultStoreConfigurationId"/> to rewind.</param>
        /// <param name="logger">For logging.</param>
        public Rewind(
            IProjectionPipeline pipeline,
            IProjectionPositions projectionPositions,
            ProjectionResultStoreConfigurationId configurationId,
            ILogger<Rewind> logger)
        {
            _pipeline = pipeline;
            _projectionPositions = projectionPositions;
            _configurationId = configurationId;
            _logger = logger;
        }

        /// <inheritdoc/>
        public string Name => "Rewind";

        /// <inheritdoc/>
        public async Task Perform(ProjectionPipelineJobStatus jobStatus)
        {
            _logger.Rewinding(_pipeline.Projection.Identifier, _configurationId);
            var resultStore = _pipeline.ResultStores[_configurationId];
            _rewindScope = resultStore.BeginRewindFor();
            jobStatus.ReportTask($"Resetting positions for '{resultStore.Name}' with configuration id {_configurationId}");
            await _projectionPositions.Reset(_pipeline.Projection, _configurationId);
        }

        /// <inheritdoc/>
        public Task PerformPostJob(ProjectionPipelineJobStatus jobStatus)
        {
            _rewindScope?.Dispose();
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task Stop() => Task.CompletedTask;
    }
}
