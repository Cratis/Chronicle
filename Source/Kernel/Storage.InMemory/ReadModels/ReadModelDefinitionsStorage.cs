// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Storage.ReadModels;

namespace Cratis.Chronicle.Storage.InMemory.ReadModels;

/// <summary>
/// Represents an in-memory implementation of <see cref="IReadModelDefinitionsStorage"/>.
/// </summary>
public sealed class ReadModelDefinitionsStorage : IReadModelDefinitionsStorage
{
    readonly ConcurrentDictionary<ReadModelIdentifier, ReadModelDefinition> _definitions = new();

    /// <inheritdoc/>
    public Task<IEnumerable<ReadModelDefinition>> GetAll() =>
        Task.FromResult<IEnumerable<ReadModelDefinition>>(_definitions.Values.ToArray());

    /// <inheritdoc/>
    public Task<bool> Has(ReadModelIdentifier identifier) =>
        Task.FromResult(_definitions.ContainsKey(identifier));

    /// <inheritdoc/>
    public Task<ReadModelDefinition> Get(ReadModelIdentifier identifier) =>
        Task.FromResult(_definitions[identifier]);

    /// <inheritdoc/>
    public Task Delete(ReadModelIdentifier identifier)
    {
        _definitions.TryRemove(identifier, out _);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task Save(ReadModelDefinition definition)
    {
        _definitions[definition.Identifier] = definition;
        return Task.CompletedTask;
    }
}
