// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Events.Projections.Json;

namespace Cratis.Events.Projections.Definitions
{
    /// <summary>
    /// Represents an implementation of <see cref="IProjectionDefinitions"/>.
    /// </summary>
    public class ProjectionDefinitions : IProjectionDefinitions
    {
        readonly IProjectionDefinitionsStorage _storage;
        readonly IJsonProjectionSerializer _projectionSerializer;
        readonly ConcurrentDictionary<ProjectionId, ProjectionDefinition> _definitions = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="ProjectionDefinition"/> class.
        /// </summary>
        /// <param name="storage"><see cref="IProjectionDefinitionsStorage"/> for stored definitions.</param>
        /// <param name="projectionSerializer"><see cref="IJsonProjectionSerializer"/> for serialization.</param>
        public ProjectionDefinitions(
            IProjectionDefinitionsStorage storage,
            IJsonProjectionSerializer projectionSerializer)
        {
            _storage = storage;
            _projectionSerializer = projectionSerializer;
        }

        /// <inheritdoc/>
        public async Task<ProjectionDefinition> GetFor(ProjectionId projectionId)
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
        public async Task Register(ProjectionDefinition definition)
        {
            _definitions[definition.Identifier] = definition;
            await _storage.Save(definition);
        }

        /// <inheritdoc/>
        public async Task<bool> HasChanged(ProjectionDefinition projectionDefinition)
        {
            if (!await HasFor(projectionDefinition.Identifier))
            {
                return true;
            }
            var incoming = _projectionSerializer.Serialize(projectionDefinition);
            var existing = _projectionSerializer.Serialize(_definitions[projectionDefinition.Identifier]);
            return incoming != existing;
        }

        async Task PopulateIfMissing(ProjectionId projectionId)
        {
            if (!_definitions.ContainsKey(projectionId))
            {
                var definitions = await _storage.GetAll();
                foreach (var definition in definitions)
                {
                    _definitions[definition.Identifier] = definition;
                }
            }
        }

        void ThrowIfMissingProjectionDefinition(ProjectionId identifier)
        {
            if (!_definitions.ContainsKey(identifier)) throw new MissingProjectionDefinition(identifier);
        }
    }
}
