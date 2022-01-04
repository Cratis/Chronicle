// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Events.Projections.Definitions;
using Orleans;

namespace Cratis.Events.Projections.Grains
{
    /// <summary>
    /// Represents an implementation of <see cref="IProjections"/>.
    /// </summary>
    public class Projections : Grain, IProjections
    {
        readonly Events.Projections.IProjections _projections;

        public Projections(Events.Projections.IProjections projections)
        {
            _projections = projections;
        }

        /// <inheritdoc/>
        public Task Register(ProjectionDefinition projectionDefinition, ProjectionPipelineDefinition pipelineDefinition) => _projections.Register(projectionDefinition, pipelineDefinition);
    }
}
