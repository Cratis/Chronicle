// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Events.Projections.Definitions;

namespace Cratis.Events.Projections
{
    /// <summary>
    /// Represents an implementation of <see cref="IProjections"/>.
    /// </summary>
    public class Projections : IProjections
    {
        readonly IProjectionDefinitions _definitions;
        readonly IProjectionFactory _projectionFactory;
        readonly IProjectionPipelineFactory _pipelineFactory;
        readonly ConcurrentDictionary<ProjectionId, IProjectionPipeline> _pipelines = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="Projections"/> class.
        /// </summary>
        /// <param name="definitions">The <see cref="IProjectionDefinitions"/> to use.</param>
        /// <param name="projectionFactory">The <see cref="IProjectionFactory"/> for creating projection instances.</param>
        /// <param name="pipelineFactory">The <see cref="IProjectionPipelineFactory"/> for creating projection pipeline instances.</param>
        public Projections(
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
            var isNew = !await _definitions.HasFor(projectionDefinition.Identifier);
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
