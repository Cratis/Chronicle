// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Chronicle.Concepts.Observation.Reducers;
using Cratis.Chronicle.Storage.Observation.Reducers;

namespace Cratis.Chronicle.Storage.InMemory.Observation.Reducers;

/// <summary>
/// Represents an in-memory implementation of <see cref="IReducerDefinitionsStorage"/>.
/// </summary>
public sealed class ReducerDefinitionsStorage : IReducerDefinitionsStorage
{
    readonly ConcurrentDictionary<ReducerId, ReducerDefinition> _definitions = new();

    /// <inheritdoc/>
    public Task<IEnumerable<ReducerDefinition>> GetAll() =>
        Task.FromResult<IEnumerable<ReducerDefinition>>(_definitions.Values.ToArray());

    /// <inheritdoc/>
    public Task<bool> Has(ReducerId id) =>
        Task.FromResult(_definitions.ContainsKey(id));

    /// <inheritdoc/>
    public Task<ReducerDefinition> Get(ReducerId id) =>
        Task.FromResult(_definitions[id]);

    /// <inheritdoc/>
    public Task Delete(ReducerId id)
    {
        _definitions.TryRemove(id, out _);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task Save(ReducerDefinition definition)
    {
        _definitions[definition.Identifier] = definition;
        return Task.CompletedTask;
    }
}
