// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Storage.Events.Constraints;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Events.Constraints;

/// <summary>
/// Represents an implementation of <see cref="IConstraintsStorage"/>.
/// </summary>
/// <param name="eventStoreDatabase">The <see cref="IEventStoreDatabase"/> to use.</param>
public class ConstraintsStorage(IEventStoreDatabase eventStoreDatabase) : IConstraintsStorage
{
    readonly IMongoCollection<StoredConstraintDefinition> _collection = eventStoreDatabase.GetCollection<StoredConstraintDefinition>(WellKnownCollectionNames.Constraints);

    /// <inheritdoc/>
    public async Task<IEnumerable<IConstraintDefinition>> GetDefinitions()
    {
        using var result = await _collection.FindAsync(_ => true);
        var definitions = await result.ToListAsync();
        return definitions
            .GroupBy(_ => _.Name)
            .Select(_ => _.OrderByDescending(d => d.Version).First().Definition)
            .ToArray();
    }

    /// <inheritdoc/>
    public async Task SaveDefinition(IConstraintDefinition definition)
    {
        var existing = await _collection
            .Find(_ => _.Name == definition.Name.Value)
            .SortByDescending(_ => _.Version)
            .FirstOrDefaultAsync();

        if (existing?.Definition.Equals(definition) == true)
        {
            return;
        }

        var nextVersion = existing is null ? 1uL : existing.Version + 1;
        var stored = new StoredConstraintDefinition($"{definition.Name}-v{nextVersion}", definition.Name.Value, nextVersion, definition);
        await _collection.InsertOneAsync(stored);
    }
}
