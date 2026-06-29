// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Chronicle.Concepts.Observation.Reactors;
using Cratis.Chronicle.Storage.Observation.Reactors;

namespace Cratis.Chronicle.Storage.InMemory.Observation.Reactors;

/// <summary>
/// Represents an in-memory implementation of <see cref="IReactorDefinitionsStorage"/>.
/// </summary>
public sealed class ReactorDefinitionsStorage : IReactorDefinitionsStorage
{
    readonly ConcurrentDictionary<ReactorId, ReactorDefinition> _definitions = new();

    /// <inheritdoc/>
    public Task<IEnumerable<ReactorDefinition>> GetAll() =>
        Task.FromResult<IEnumerable<ReactorDefinition>>(_definitions.Values.ToArray());

    /// <inheritdoc/>
    public Task<bool> Has(ReactorId id) =>
        Task.FromResult(_definitions.ContainsKey(id));

    /// <inheritdoc/>
    public Task<ReactorDefinition> Get(ReactorId id) =>
        Task.FromResult(_definitions[id]);

    /// <inheritdoc/>
    public Task Delete(ReactorId id)
    {
        _definitions.TryRemove(id, out _);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task Save(ReactorDefinition definition)
    {
        _definitions[definition.Identifier] = definition;
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task Rename(ReactorId currentId, ReactorId newId)
    {
        if (!_definitions.TryRemove(currentId, out var existing))
        {
            return Task.CompletedTask;
        }

        _definitions[newId] = existing with { Identifier = newId };
        return Task.CompletedTask;
    }
}
