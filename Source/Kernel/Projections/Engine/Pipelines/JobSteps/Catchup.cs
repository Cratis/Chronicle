// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Aksio.Cratis.Events.Projections.Pipelines.JobSteps
{
    /// <summary>
    /// Represents a <see cref="IProjectionPipelineJobStep"/> for catching up.
    /// </summary>
    public class Catchup : IProjectionPipelineJobStep
    {
        readonly IProjectionPipeline _pipeline;
        readonly IProjectionPositions _projectionPositions;
        readonly IProjectionEventProvider _projectionEventProvider;
        readonly IProjectionPipelineHandler _projectionPipelineHandler;
        readonly ProjectionSinkConfigurationId _configurationId;
        readonly ILogger<Catchup> _logger;

        /// <inheritdoc/>
        public string Name => "Catchup";

        /// <summary>
        /// Initializes a new instance of the <see cref="Rewind"/> class.
        /// </summary>
        /// <param name="pipeline"><see cref="IProjectionPipeline"/> the rewind is for.</param>
        /// <param name="projectionPositions"><see cref="IProjectionPositions"/> for working with the positions of the projections.</param>
        /// <param name="projectionEventProvider"><see cref="IProjectionEventProvider"/> for providing events.</param>
        /// <param name="projectionPipelineHandler"><see cref="IProjectionPipelineHandler"/> for handling events for the pipeline.</param>
        /// <param name="configurationId"><see cref="ProjectionSinkConfigurationId"/> to rewind.</param>
        /// <param name="logger">For logging.</param>
        public Catchup(
            IProjectionPipeline pipeline,
            IProjectionPositions projectionPositions,
            IProjectionEventProvider projectionEventProvider,
            IProjectionPipelineHandler projectionPipelineHandler,
            ProjectionSinkConfigurationId configurationId,
            ILogger<Catchup> logger)
        {
            _pipeline = pipeline;
            _projectionPositions = projectionPositions;
            _projectionEventProvider = projectionEventProvider;
            _projectionPipelineHandler = projectionPipelineHandler;
            _configurationId = configurationId;
            _logger = logger;
        }

        /// <inheritdoc/>
        public async Task Perform(ProjectionPipelineJobStatus jobStatus)
        {
            _logger.CatchingUp(_pipeline.Projection.Identifier, _configurationId);
            var sink = _pipeline.Sinks[_configurationId];
            jobStatus.ReportTask($"Getting positions for '{sink.Name}' with configuration id {_configurationId}");
            var offset = await _projectionPositions.GetFor(_pipeline.Projection, _configurationId);
            if (offset == 0)
            {
                jobStatus.ReportTask($"Prepare for initial run for '{sink.Name}' with configuration id {_configurationId}");
                await sink.PrepareInitialRun();
            }

            var exhausted = false;

            jobStatus.ReportTask($"Catching up from offset {offset} for '{sink.Name}' with configuration id {_configurationId}");

            while (!exhausted)
            {
                var cursor = await _projectionEventProvider.GetFromSequenceNumber(_pipeline.Projection, offset);
                while (await cursor.MoveNext())
                {
                    if (!cursor.Current.Any())
                    {
                        exhausted = true;
                        break;
                    }

                    foreach (var @event in cursor.Current)
                    {
                        offset = await _projectionPipelineHandler.Handle(@event, _pipeline, sink, _configurationId);
                    }
                }
                if (!cursor.Current.Any()) exhausted = true;
            }

            jobStatus.ReportTask($"Caught up for '{sink.Name}' with configuration id {_configurationId}");
        }

        /// <inheritdoc/>
        public Task PerformPostJob(ProjectionPipelineJobStatus jobStatus)
        {
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task Stop() => Task.CompletedTask;
    }
}
