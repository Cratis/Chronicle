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
    readonly IMongoCollection<IConstraintDefinition> _collection = eventStoreDatabase.GetCollection<IConstraintDefinition>(WellKnownCollectionNames.Constraints);

    /// <inheritdoc/>
    public async Task<IEnumerable<IConstraintDefinition>> GetDefinitions()
    {
        var result = await _collection.FindAsync(_ => true);
        return result.ToList();
    }

    /// <inheritdoc/>
    public async Task SaveDefinition(IConstraintDefinition definition)
    {
        var filter = Builders<IConstraintDefinition>.Filter.Eq(new StringFieldDefinition<IConstraintDefinition, string>("name"), definition.Name.Value);
        await _collection.ReplaceOneAsync(filter, definition, new ReplaceOptions { IsUpsert = true });
    }
}
