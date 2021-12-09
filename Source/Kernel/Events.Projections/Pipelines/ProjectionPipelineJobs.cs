// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Events.Projections.Pipelines.JobSteps;
using Microsoft.Extensions.Logging;

namespace Cratis.Events.Projections.Pipelines
{
    /// <summary>
    /// Represents an implementation of <see cref="IProjectionPipelineJobs"/>.
    /// </summary>
    public class ProjectionPipelineJobs : IProjectionPipelineJobs
    {
        readonly IProjectionPositions _projectionPositions;
        readonly IProjectionEventProvider _projectionEventProvider;
        readonly IProjectionPipelineHandler _projectionPipelineHandler;
        readonly ILoggerFactory _loggerFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProjectionPipelineJobs"/> class.
        /// </summary>
        /// <param name="projectionPositions"><see cref="IProjectionPositions"/> for working with the positions of the projections.</param>
        /// <param name="projectionEventProvider"><see cref="IProjectionEventProvider"/> for providing events.</param>
        /// <param name="projectionPipelineHandler"><see cref="IProjectionPipelineHandler"/> for handling events for the pipeline.</param>
        /// <param name="loggerFactory">For creating loggers.</param>
        public ProjectionPipelineJobs(
            IProjectionPositions projectionPositions,
            IProjectionEventProvider projectionEventProvider,
            IProjectionPipelineHandler projectionPipelineHandler,
            ILoggerFactory loggerFactory)
        {
            _projectionPositions = projectionPositions;
            _projectionEventProvider = projectionEventProvider;
            _projectionPipelineHandler = projectionPipelineHandler;
            _loggerFactory = loggerFactory;
        }

        /// <inheritdoc/>
        public IProjectionPipelineJob Catchup(IProjectionPipeline pipeline, ProjectionResultStoreConfigurationId configurationId) =>
            new ProjectionPipelineJob(
                "Rewind",
                new[] {
                    new Catchup(
                        pipeline,
                        _projectionPositions,
                        _projectionEventProvider,
                        _projectionPipelineHandler,
                        configurationId,
                        _loggerFactory.CreateLogger<Catchup>())
                    });

        /// <inheritdoc/>
        public IEnumerable<IProjectionPipelineJob> Catchup(IProjectionPipeline pipeline) =>
            pipeline.ResultStores.Select(kvp =>
                new ProjectionPipelineJob(
                    "Rewind",
                    new[] {
                        new Catchup(
                            pipeline,
                            _projectionPositions,
                            _projectionEventProvider,
                            _projectionPipelineHandler,
                            kvp.Key,
                            _loggerFactory.CreateLogger<Catchup>())
                    }));

        /// <inheritdoc/>
        public IProjectionPipelineJob Rewind(IProjectionPipeline pipeline, ProjectionResultStoreConfigurationId configurationId) =>
            new ProjectionPipelineJob(
                "Rewind",
                new IProjectionPipelineJobStep[] {
                    new Rewind(
                        pipeline,
                        _projectionPositions,
                        configurationId,
                        _loggerFactory.CreateLogger<Rewind>()),
                    new Catchup(
                        pipeline,
                        _projectionPositions,
                        _projectionEventProvider,
                        _projectionPipelineHandler,
                        configurationId,
                        _loggerFactory.CreateLogger<Catchup>())
                });

        /// <inheritdoc/>
        public IEnumerable<IProjectionPipelineJob> Rewind(IProjectionPipeline pipeline) => pipeline.ResultStores.Select(kvp =>
            new ProjectionPipelineJob(
                "Rewind",
                new IProjectionPipelineJobStep[] {
                    new Rewind(
                        pipeline,
                        _projectionPositions,
                        kvp.Key,
                        _loggerFactory.CreateLogger<Rewind>()),
                    new Catchup(
                        pipeline,
                        _projectionPositions,
                        _projectionEventProvider,
                        _projectionPipelineHandler,
                        kvp.Key,
                        _loggerFactory.CreateLogger<Catchup>())
                }));
    }
}
