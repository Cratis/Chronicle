// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Chronicle.Concepts.SequenceQueries;
using Cratis.Chronicle.Storage.SequenceQueries;
using Cratis.Reactive;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.SequenceQueries;

/// <summary>
/// Represents a <see cref="ISequenceQueryStorage"/> for saved event sequence queries in MongoDB.
/// </summary>
/// <param name="eventStoreDatabase">The <see cref="IEventStoreDatabase"/>.</param>
public class SequenceQueryStorage(IEventStoreDatabase eventStoreDatabase) : ISequenceQueryStorage
{
    IMongoCollection<SequenceQueryDefinition> Collection =>
        eventStoreDatabase.GetCollection<SequenceQueryDefinition>(WellKnownCollectionNames.SequenceQueries);

    /// <inheritdoc/>
    public async Task<IEnumerable<Concepts.SequenceQueries.SequenceQueryDefinition>> GetAllFor(SequenceQueryOwner owner)
    {
        var ownerValue = owner.Value;
        using var result = await Collection.FindAsync(_ => _.Scope == SequenceQueryScope.Everyone || _.Owner == ownerValue);
        var definitions = await result.ToListAsync();

        return definitions.Select(_ => _.ToKernel()).ToArray();
    }

    /// <inheritdoc/>
    public ISubject<IEnumerable<Concepts.SequenceQueries.SequenceQueryDefinition>> ObserveAllFor(SequenceQueryOwner owner)
    {
        var ownerValue = owner.Value;

        return new TransformingSubject<IEnumerable<SequenceQueryDefinition>, IEnumerable<Concepts.SequenceQueries.SequenceQueryDefinition>>(
            Collection.Observe(_ => _.Scope == SequenceQueryScope.Everyone || _.Owner == ownerValue),
            definitions => definitions.Select(_ => _.ToKernel()));
    }

    /// <inheritdoc/>
    public Task Save(Concepts.SequenceQueries.SequenceQueryDefinition definition)
    {
        var id = definition.Id.Value;

        return Collection.ReplaceOneAsync(
            filter: _ => _.Id == id,
            replacement: definition.ToMongoDB(),
            options: new ReplaceOptions { IsUpsert = true });
    }

    /// <inheritdoc/>
    public Task Delete(SequenceQueryId id)
    {
        var value = id.Value;

        return Collection.DeleteOneAsync(_ => _.Id == value);
    }
}
