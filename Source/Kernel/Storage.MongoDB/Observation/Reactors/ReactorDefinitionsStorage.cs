// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.MongoDB;
using Cratis.Chronicle.Concepts.Observation.Reactors;
using Cratis.Chronicle.Storage.Observation.Reactors;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Observation.Reactors;

/// <summary>
/// Represents a <see cref="IReactorDefinitionsStorage"/> for projection definitions in MongoDB.
/// </summary>
/// <remarks>
/// Initializes a new instance of <see cref="IMongoDBClientFactory"/>.
/// </remarks>
/// <param name="eventStoreDatabase">The <see cref="IEventStoreDatabase"/>.</param>
public class ReactorDefinitionsStorage(
    IEventStoreDatabase eventStoreDatabase) : IReactorDefinitionsStorage
{
    IMongoCollection<ReactorDefinition> Collection => eventStoreDatabase.GetCollection<ReactorDefinition>(WellKnownCollectionNames.ReactorDefinitions);

    /// <inheritdoc/>
    public async Task<IEnumerable<Concepts.Observation.Reactors.ReactorDefinition>> GetAll()
    {
        using var result = await Collection.FindAsync(FilterDefinition<ReactorDefinition>.Empty);
        var definitions = result.ToList();
        return definitions.Select(definition => definition.ToKernel()).ToArray();
    }

    /// <inheritdoc/>
    public Task<bool> Has(ReactorId id) =>
        Collection.Find(r => r.Id == id).AnyAsync();

    /// <inheritdoc/>
    public async Task<Concepts.Observation.Reactors.ReactorDefinition> Get(ReactorId id)
    {
        using var result = await Collection.FindAsync(definition => definition.Id == id);
        return result.Single().ToKernel();
    }

    /// <inheritdoc/>
    public Task Delete(ReactorId id) =>
        Collection.DeleteOneAsync(definition => definition.Id == id);

    /// <inheritdoc/>
    public Task Save(Concepts.Observation.Reactors.ReactorDefinition definition) =>
        Collection.ReplaceOneAsync(
            filter: def => def.Id == definition.Identifier,
            replacement: definition.ToMongoDB(),
            options: new ReplaceOptions { IsUpsert = true });

    /// <inheritdoc/>
    public async Task Rename(ReactorId currentId, ReactorId newId)
    {
        using var result = await Collection.FindAsync(definition => definition.Id == currentId);
        var definition = await result.FirstOrDefaultAsync();
        if (definition is null)
        {
            return;
        }

        await Collection.DeleteOneAsync(def => def.Id == currentId);
        var kernelDefinition = definition.ToKernel();
        await Save(kernelDefinition with { Identifier = newId });
    }
}
