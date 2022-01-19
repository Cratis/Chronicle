// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Aksio.Cratis.Execution;

namespace Aksio.Cratis.Events.Projections.Definitions
{
    /// <summary>
    /// Represents an implementation of <see cref="IProjectionPipelineDefinitions"/>.
    /// </summary>
    [Singleton]
    public class ProjectionPipelineDefinitions : IProjectionPipelineDefinitions
    {
        readonly IProjectionPipelineDefinitionsStorage _storage;
        readonly ConcurrentDictionary<ProjectionId, ProjectionPipelineDefinition> _definitions = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="ProjectionDefinition"/> class.
        /// </summary>
        /// <param name="storage"><see cref="IProjectionPipelineDefinitionsStorage"/> for stored definitions.</param>
        public ProjectionPipelineDefinitions(
            IProjectionPipelineDefinitionsStorage storage)
        {
            _storage = storage;
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<ProjectionPipelineDefinition>> GetAll()
        {
            await PopulateIfEmpty();
            return _definitions.Values;
        }

        /// <inheritdoc/>
        public async Task<ProjectionPipelineDefinition> GetFor(ProjectionId projectionId)
        {
            await PopulateIfMissing(projectionId);
            ThrowIfMissingProjectionDefinition(projectionId);
            return _definitions[projectionId];
        }

        /// <inheritdoc/>
        public async Task<bool> HasFor(ProjectionId projectionId)
        {
            await PopulateIfMissing(projectionId);
            return _definitions.ContainsKey(projectionId);
        }

        /// <inheritdoc/>
        public async Task Register(ProjectionPipelineDefinition definition)
        {
            _definitions[definition.ProjectionId] = definition;
            await _storage.Save(definition);
        }

        async Task PopulateIfMissing(ProjectionId projectionId)
        {
            if (!_definitions.ContainsKey(projectionId))
            {
                await Populate();
            }
        }

        async Task PopulateIfEmpty()
        {
            if (_definitions.IsEmpty)
            {
                await Populate();
            }
        }

        async Task Populate()
        {
            var definitions = await _storage.GetAll();
            foreach (var definition in definitions)
            {
                _definitions[definition.ProjectionId] = definition;
            }
        }

        void ThrowIfMissingProjectionDefinition(ProjectionId identifier)
        {
            if (!_definitions.ContainsKey(identifier)) throw new MissingProjectionPipelineDefinition(identifier);
        }
    }
}
