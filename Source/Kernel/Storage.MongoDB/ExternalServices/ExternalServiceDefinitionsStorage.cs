// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Chronicle.Concepts.ExternalServices;
using Cratis.Chronicle.Storage.ExternalServices;
using Cratis.Reactive;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.ExternalServices;

/// <summary>
/// Represents a <see cref="IExternalServiceDefinitionsStorage"/> for external service definitions in MongoDB.
/// </summary>
/// <param name="eventStoreDatabase">The <see cref="IEventStoreDatabase"/>.</param>
public class ExternalServiceDefinitionsStorage(
    IEventStoreDatabase eventStoreDatabase) : IExternalServiceDefinitionsStorage
{
    IMongoCollection<ExternalServiceDefinition> Collection => eventStoreDatabase.GetCollection<ExternalServiceDefinition>(WellKnownCollectionNames.ExternalServiceDefinitions);

    /// <inheritdoc/>
    public async Task<IEnumerable<Concepts.ExternalServices.ExternalServiceDefinition>> GetAll()
    {
        using var result = await Collection.FindAsync(FilterDefinition<ExternalServiceDefinition>.Empty);
        var definitions = await result.ToListAsync();
        return definitions.Select(definition => definition.ToKernel()).ToArray();
    }

    /// <inheritdoc/>
    public ISubject<IEnumerable<Concepts.ExternalServices.ExternalServiceDefinition>> ObserveAll() =>
        new TransformingSubject<IEnumerable<ExternalServiceDefinition>, IEnumerable<Concepts.ExternalServices.ExternalServiceDefinition>>(
            Collection.Observe(),
            definitions => definitions.Select(definition => definition.ToKernel()));

    /// <inheritdoc/>
    public Task<bool> Has(ExternalServiceId id) =>
        Collection.Find(r => r.Id == id).AnyAsync();

    /// <inheritdoc/>
    public async Task<Concepts.ExternalServices.ExternalServiceDefinition> Get(ExternalServiceId id)
    {
        using var result = await Collection.FindAsync(definition => definition.Id == id);
        return (await result.SingleAsync()).ToKernel();
    }

    /// <inheritdoc/>
    public Task Delete(ExternalServiceId id) =>
        Collection.DeleteOneAsync(definition => definition.Id == id);

    /// <inheritdoc/>
    public Task Save(Concepts.ExternalServices.ExternalServiceDefinition definition) =>
        Collection.ReplaceOneAsync(
            filter: def => def.Id == definition.Id,
            replacement: definition.ToMongoDB(),
            options: new ReplaceOptions { IsUpsert = true });
}
