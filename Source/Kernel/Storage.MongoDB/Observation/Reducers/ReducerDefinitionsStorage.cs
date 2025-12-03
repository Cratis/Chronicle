// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.MongoDB;
using Cratis.Chronicle.Concepts.Observation.Reducers;
using Cratis.Chronicle.Storage.Observation.Reducers;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Observation.Reducers;

/// <summary>
/// Represents a <see cref="IReducerDefinitionsStorage"/> for projection definitions in MongoDB.
/// </summary>
/// <remarks>
/// Initializes a new instance of <see cref="IMongoDBClientFactory"/>.
/// </remarks>
/// <param name="eventStoreDatabase">The <see cref="IEventStoreDatabase"/>.</param>
public class ReducerDefinitionsStorage(
    IEventStoreDatabase eventStoreDatabase) : IReducerDefinitionsStorage
{
    IMongoCollection<ReducerDefinition> Collection => eventStoreDatabase.GetCollection<ReducerDefinition>(WellKnownCollectionNames.ReducerDefinitions);

    /// <inheritdoc/>
    public async Task<IEnumerable<Concepts.Observation.Reducers.ReducerDefinition>> GetAll()
    {
        using var result = await Collection.FindAsync(FilterDefinition<ReducerDefinition>.Empty);
        var definitions = result.ToList();
        return definitions.Select(definition => definition.ToKernel()).ToArray();
    }

    /// <inheritdoc/>
    public Task<bool> Has(ReducerId id) =>
        Collection.Find(r => r.Id == id).AnyAsync();

    /// <inheritdoc/>
    public async Task<Concepts.Observation.Reducers.ReducerDefinition> Get(ReducerId id)
    {
        using var result = await Collection.FindAsync(definition => definition.Id == id);
        return result.Single().ToKernel();
    }

    /// <inheritdoc/>
    public Task Delete(ReducerId id) =>
        Collection.DeleteOneAsync(definition => definition.Id == id);

    /// <inheritdoc/>
    public Task Save(Concepts.Observation.Reducers.ReducerDefinition definition) =>
        Collection.ReplaceOneAsync(
            filter: def => def.Id == definition.Identifier,
            replacement: definition.ToMongoDB(),
            options: new ReplaceOptions { IsUpsert = true });
}
