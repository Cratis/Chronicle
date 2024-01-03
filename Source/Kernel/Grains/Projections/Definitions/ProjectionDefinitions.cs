// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Kernel.Persistence.Projections;
using Aksio.Cratis.Projections;
using Aksio.Cratis.Projections.Definitions;
using Aksio.Cratis.Projections.Json;

namespace Aksio.Cratis.Kernel.Grains.Projections.Definitions;

/// <summary>
/// Represents an implementation of <see cref="IProjectionDefinitions"/>.
/// </summary>
[SingletonPerMicroservice]
public class ProjectionDefinitions : IProjectionDefinitions
{
    readonly IProjectionDefinitionsStorage _storage;
    readonly IJsonProjectionSerializer _projectionSerializer;
    readonly Dictionary<ProjectionId, ProjectionDefinition> _definitions = new();

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
    public async Task<IEnumerable<ProjectionDefinition>> GetAll()
    {
        await PopulateIfEmpty();
        return _definitions.Values;
    }

    /// <inheritdoc/>
    public async Task<(bool Found, ProjectionDefinition? Projection)> TryGetFor(ProjectionId projectionId)
    {
        await PopulateIfMissing(projectionId);
        if (!_definitions.ContainsKey(projectionId))
        {
            return (false, null);
        }

        return (true, _definitions[projectionId]);
    }

    /// <inheritdoc/>
    public async Task Register(ProjectionDefinition definition)
    {
        definition = definition with { LastUpdated = DateTimeOffset.UtcNow };
        _definitions[definition.Identifier] = definition;

        await _storage.Save(definition);
    }

    /// <inheritdoc/>
    public async Task<(bool IsNew, bool HasChanged)> IsNewOrChanged(ProjectionDefinition projectionDefinition)
    {
        await PopulateIfMissing(projectionDefinition.Identifier);
        if (!_definitions.ContainsKey(projectionDefinition.Identifier))
        {
            return (true, false);
        }

        var incoming = projectionDefinition with { LastUpdated = null };
        var existing = _definitions[projectionDefinition.Identifier] with { LastUpdated = null };
        var incomingJson = _projectionSerializer.Serialize(incoming);
        var existingJson = _projectionSerializer.Serialize(existing);
        var incomingAsJsonString = incomingJson.ToJsonString();
        var existingAsJsonString = existingJson.ToJsonString();
        var changed = !incomingAsJsonString.Equals(existingAsJsonString);
        return (false, changed);
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
        if (_definitions.Count == 0)
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
