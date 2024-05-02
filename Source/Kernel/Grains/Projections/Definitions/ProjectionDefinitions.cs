// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Kernel.Storage.Projections;
using Cratis.Projections;
using Cratis.Projections.Definitions;
using Cratis.Projections.Json;

namespace Cratis.Kernel.Grains.Projections.Definitions;

/// <summary>
/// Represents an implementation of <see cref="IProjectionDefinitions"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ProjectionDefinition"/> class.
/// </remarks>
/// <param name="storage"><see cref="IProjectionDefinitionsStorage"/> for stored definitions.</param>
/// <param name="projectionSerializer"><see cref="IJsonProjectionSerializer"/> for serialization.</param>
public class ProjectionDefinitions(
    IProjectionDefinitionsStorage storage,
    IJsonProjectionSerializer projectionSerializer) : IProjectionDefinitions
{
    readonly Dictionary<ProjectionId, ProjectionDefinition> _definitions = [];

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
        if (!_definitions.TryGetValue(projectionId, out var value))
        {
            return (false, null);
        }

        return (true, value);
    }

    /// <inheritdoc/>
    public async Task Register(ProjectionDefinition definition)
    {
        definition = definition with { LastUpdated = DateTimeOffset.UtcNow };
        _definitions[definition.Identifier] = definition;

        await storage.Save(definition);
    }

    /// <inheritdoc/>
    public async Task<(bool IsNew, bool HasChanged)> IsNewOrChanged(ProjectionDefinition projectionDefinition)
    {
        await PopulateIfMissing(projectionDefinition.Identifier);
        if (!_definitions.TryGetValue(projectionDefinition.Identifier, out var value))
        {
            return (true, false);
        }

        var incoming = projectionDefinition with { LastUpdated = null };
        var existing = value with { LastUpdated = null };
        var incomingJson = projectionSerializer.Serialize(incoming);
        var existingJson = projectionSerializer.Serialize(existing);
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
        var definitions = await storage.GetAll();
        foreach (var definition in definitions)
        {
            _definitions[definition.Identifier] = definition;
        }
    }
}
