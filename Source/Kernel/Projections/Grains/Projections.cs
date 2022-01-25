// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events.Projections.Definitions;
using Orleans;

namespace Aksio.Cratis.Events.Projections.Grains
{
    /// <summary>
    /// Represents an implementation of <see cref="IProjections"/>.
    /// </summary>
    public class Projections : Grain, IProjections
    {
        readonly Events.Projections.IProjections _projections;

        /// <summary>
        /// Initializes a new instance of the <see cref="Projections"/> clas.
        /// </summary>
        /// <param name="projections">The underlying <see cref="Events.Projections.IProjections"/>.</param>
        public Projections(Events.Projections.IProjections projections)
        {
            _projections = projections;
        }

        /// <inheritdoc/>
        public Task Register(ProjectionDefinition projectionDefinition, ProjectionPipelineDefinition pipelineDefinition) => _projections.Register(projectionDefinition, pipelineDefinition);

        /// <inheritdoc/>
        public Task Start()
        {
            _projections.Start();
            return Task.CompletedTask;
        }
    }
}
