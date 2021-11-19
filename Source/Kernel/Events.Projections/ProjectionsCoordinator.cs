// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Events.Projections.Definitions;

namespace Cratis.Events.Projections
{
    /// <summary>
    /// Represents an implementation of <see cref="IProjectionsCoordinator"/>.
    /// </summary>
    public class ProjectionsCoordinator : IProjectionsCoordinator
    {
        readonly IProjectionDefinitions _definitions;
        readonly IProjectionFactory _projectionFactory;
        readonly IProjectionPipelineFactory _pipelineFactory;

        readonly ConcurrentDictionary<ProjectionId, IProjectionPipeline> _pipelines = new();

        public ProjectionsCoordinator(
            IProjectionDefinitions definitions,
            IProjectionFactory projectionFactory,
            IProjectionPipelineFactory pipelineFactory)
        {
            _definitions = definitions;
            _projectionFactory = projectionFactory;
            _pipelineFactory = pipelineFactory;
        }

        /// <inheritdoc/>
        public async Task Register(ProjectionDefinition projectionDefinition, ProjectionPipelineDefinition pipelineDefinition)
        {
            var projection = _projectionFactory.CreateFrom(projectionDefinition);
            var pipeline = _pipelineFactory.CreateFrom(projection, pipelineDefinition);
            var isNew = await _definitions.HasFor(projectionDefinition.Identifier);
            var hasChanged = await _definitions.HasChanged(projectionDefinition);

            if (!isNew && hasChanged)
            {
                pipeline.Rewind();
            }

            await _definitions.Register(projectionDefinition);

            _pipelines[projection.Identifier] = pipeline;
        }

        /// <inheritdoc/>
        public void Start()
        {
            foreach (var (_, pipeline) in _pipelines)
            {
                pipeline.Start();
            }
        }
    }
}
