// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;

namespace Cratis.Chronicle.Storage.Projections.for_CachingProjectionDefinitionsStorage.given;

/// <summary>
/// Represents the storage that the projection-definition caches of several silos share.
/// </summary>
public class SharedProjectionDefinitionsStorage : IProjectionDefinitionsStorage
{
    readonly ConcurrentDictionary<ProjectionId, ProjectionDefinition> _definitions = new();

    /// <inheritdoc/>
    public Task<IEnumerable<ProjectionDefinition>> GetAll() => Task.FromResult<IEnumerable<ProjectionDefinition>>(_definitions.Values.ToArray());

    /// <inheritdoc/>
    public Task<bool> Has(ProjectionId id) => Task.FromResult(_definitions.ContainsKey(id));

    /// <inheritdoc/>
    public Task<ProjectionDefinition> Get(ProjectionId id) => Task.FromResult(_definitions[id]);

    /// <inheritdoc/>
    public Task Delete(ProjectionId id)
    {
        _definitions.TryRemove(id, out _);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task Save(ProjectionDefinition definition)
    {
        _definitions[definition.Identifier] = definition;
        return Task.CompletedTask;
    }
}
