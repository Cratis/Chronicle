// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Events.Projections.Definitions;
using Cratis.Execution;

namespace Cratis.Events.Projections
{
    /// <summary>
    /// Represents an implementation of <see cref="IProjections"/>.
    /// </summary>
    [Singleton]
    public class Projections : IProjections
    {
        readonly IProjectionDefinitions _projectionDefinitions;
        readonly IProjectionPipelineDefinitions _projectionPipelineDefinitions;
        readonly IProjectionFactory _projectionFactory;
        readonly IProjectionPipelineFactory _pipelineFactory;
        readonly ConcurrentDictionary<ProjectionId, IProjectionPipeline> _pipelines = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="Projections"/> class.
        /// </summary>
        /// <param name="projectionDefinitions">The <see cref="IProjectionDefinitions"/> to use.</param>
        /// <param name="projectionPipelineDefinitions">The <see cref="IProjectionPipelineDefinitions"/> to use.</param>
        /// <param name="projectionFactory">The <see cref="IProjectionFactory"/> for creating projection instances.</param>
        /// <param name="pipelineFactory">The <see cref="IProjectionPipelineFactory"/> for creating projection pipeline instances.</param>
        public Projections(
            IProjectionDefinitions projectionDefinitions,
            IProjectionPipelineDefinitions projectionPipelineDefinitions,
            IProjectionFactory projectionFactory,
            IProjectionPipelineFactory pipelineFactory)
        {
            _projectionDefinitions = projectionDefinitions;
            _projectionPipelineDefinitions = projectionPipelineDefinitions;
            _projectionFactory = projectionFactory;
            _pipelineFactory = pipelineFactory;
        }

        /// <inheritdoc/>
        public async Task Register(ProjectionDefinition projectionDefinition, ProjectionPipelineDefinition pipelineDefinition)
        {
            var projection = _projectionFactory.CreateFrom(projectionDefinition);
            var pipeline = _pipelineFactory.CreateFrom(projection, pipelineDefinition);
            var isNew = !await _projectionDefinitions.HasFor(projectionDefinition.Identifier);
            var hasChanged = await _projectionDefinitions.HasChanged(projectionDefinition);

            if (!isNew && hasChanged)
            {
                pipeline.Rewind();
            }

            await _projectionDefinitions.Register(projectionDefinition);
            await _projectionPipelineDefinitions.Register(pipelineDefinition);

            _pipelines[projection.Identifier] = pipeline;
        }

        /// <inheritdoc/>
        public void Start()
        {
            RegisterUnregisteredProjections().Wait();
            foreach (var (_, pipeline) in _pipelines)
            {
                pipeline.Start();
            }
        }

        /// <inheritdoc/>
        public IEnumerable<IProjectionPipeline> GetAll() => _pipelines.Values;

        /// <inheritdoc/>
        public IProjectionPipeline GetById(ProjectionId id) => _pipelines[id];

        async Task RegisterUnregisteredProjections()
        {
            var projectionPipelineDefinitions = _projectionPipelineDefinitions.GetAll();
            foreach (var pipeline in projectionPipelineDefinitions.Where(pipeline => !_pipelines.Any(kvp => kvp.Key.Equals(pipeline.ProjectionId))))
            {
                if (await _projectionDefinitions.HasFor(pipeline.ProjectionId))
                {
                    var projection = await _projectionDefinitions.GetFor(pipeline.ProjectionId);
                    await Register(projection, pipeline);
                }
            }
        }
    }
}
