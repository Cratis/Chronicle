// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Aksio.Cratis.Events.Projections.Json;
using Aksio.Cratis.Execution;

namespace Aksio.Cratis.Events.Projections.Definitions;

/// <summary>
/// Represents an implementation of <see cref="IProjectionDefinitions"/>.
/// </summary>
[SingletonPerMicroservice]
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
    public IEnumerable<ProjectionDefinition> GetAll()
    {
        PopulateIfEmpty().Wait();
        return _definitions.Values;
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
            await Populate();
        }
    }

    void ThrowIfMissingProjectionDefinition(ProjectionId identifier)
    {
        if (!_definitions.ContainsKey(identifier)) throw new MissingProjectionDefinition(identifier);
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
            _definitions[definition.Identifier] = definition;
        }
    }
}
