// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Aksio.Cratis.Events.Projections.Definitions;
using Orleans;
using EngineProjection = Aksio.Cratis.Events.Projections.IProjection;

namespace Aksio.Cratis.Events.Projections.Grains
{
    /// <summary>
    /// Represents an implementation of <see cref="IProjection"/>.
    /// </summary>
    public class Projection : Grain, IProjection
    {
        readonly IProjectionDefinitions _projectionDefinitions;
        readonly IProjectionFactory _projectionFactory;
        EngineProjection? _projection;

        /// <summary>
        /// Initializes a new instance of the <see cref="Projection"/> class.
        /// </summary>
        /// <param name="projectionDefinitions"><see cref="IProjectionDefinitions"/>.</param>
        /// <param name="projectionFactory"><see cref="IProjectionFactory"/> for creating engine projections.</param>
        public Projection(
            IProjectionDefinitions projectionDefinitions,
            IProjectionFactory projectionFactory)
        {
            _projectionDefinitions = projectionDefinitions;
            _projectionFactory = projectionFactory;
        }

        /// <inheritdoc/>
        public override async Task OnActivateAsync()
        {
            var projectionId = this.GetPrimaryKey();
            var definition = await _projectionDefinitions.GetFor(projectionId);
            _projection = await _projectionFactory.CreateFrom(definition);
        }

        /// <inheritdoc/>
        public Task<JsonObject> GetModelInstanceById(EventSourceId eventSourceId)
        {
            Console.WriteLine(_projection?.Name ?? string.Empty);
            return Task.FromResult(new JsonObject());
        }
    }
}
