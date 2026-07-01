// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Storage.Projections;

namespace Cratis.Chronicle.Storage.InMemory.Projections;

/// <summary>
/// Represents an in-memory implementation of <see cref="IProjectionDefinitionsStorage"/>.
/// </summary>
public sealed class ProjectionDefinitionsStorage : IProjectionDefinitionsStorage
{
    readonly ConcurrentDictionary<ProjectionId, ProjectionDefinition> _definitions = new();

    /// <inheritdoc/>
    public Task<IEnumerable<ProjectionDefinition>> GetAll() =>
        Task.FromResult<IEnumerable<ProjectionDefinition>>(_definitions.Values.ToArray());

    /// <inheritdoc/>
    public Task<bool> Has(ProjectionId id) =>
        Task.FromResult(_definitions.ContainsKey(id));

    /// <inheritdoc/>
    public Task<ProjectionDefinition> Get(ProjectionId id) =>
        Task.FromResult(_definitions[id]);

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
