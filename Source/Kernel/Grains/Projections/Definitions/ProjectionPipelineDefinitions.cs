// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Projections.Definitions;
using Cratis.Chronicle.Storage.Projections;

namespace Cratis.Chronicle.Grains.Projections.Definitions;

/// <summary>
/// Represents an implementation of <see cref="IProjectionPipelineDefinitions"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ProjectionDefinition"/> class.
/// </remarks>
/// <param name="storage"><see cref="IProjectionPipelineDefinitionsStorage"/> for stored definitions.</param>
public class ProjectionPipelineDefinitions(
    IProjectionPipelineDefinitionsStorage storage) : IProjectionPipelineDefinitions
{
    readonly Dictionary<ProjectionId, ProjectionPipelineDefinition> _definitions = [];

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
        await storage.Save(definition);
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
            _definitions[definition.ProjectionId] = definition;
        }
    }

    void ThrowIfMissingProjectionDefinition(ProjectionId identifier)
    {
        if (!_definitions.ContainsKey(identifier)) throw new MissingProjectionPipelineDefinition(identifier);
    }
}
