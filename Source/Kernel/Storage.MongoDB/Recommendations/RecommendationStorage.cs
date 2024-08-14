// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Concepts.Recommendations;
using Cratis.Chronicle.Storage.Recommendations;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Recommendations;

/// <summary>
/// Represents an implementation of <see cref="IRecommendationStorage"/> for MongoDB.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="RecommendationStorage"/> class.
/// </remarks>
/// <param name="database">Provider for <see cref="IEventStoreNamespaceDatabase"/>.</param>
public class RecommendationStorage(IEventStoreNamespaceDatabase database) : IRecommendationStorage
{
    IMongoCollection<RecommendationState> Collection => database.GetCollection<RecommendationState>(WellKnownCollectionNames.Recommendations);

    /// <inheritdoc/>
    public async Task<RecommendationState?> Get(RecommendationId recommendationId)
    {
        var filter = GetIdFilter(recommendationId);
        var cursor = await Collection.FindAsync(filter).ConfigureAwait(false);
        return cursor.SingleOrDefault();
    }

    /// <inheritdoc/>
    public Task Remove(RecommendationId recommendationId)
    {
        var filter = GetIdFilter(recommendationId);
        return Collection.DeleteOneAsync(filter);
    }

    /// <inheritdoc/>
    public async Task Save(RecommendationId recommendationId, RecommendationState recommendationState) =>
        await Collection.ReplaceOneAsync(GetIdFilter(recommendationId), recommendationState, new ReplaceOptions { IsUpsert = true });

    /// <inheritdoc/>
    public async Task<IImmutableList<RecommendationState>> GeAll()
    {
        var cursor = await Collection.FindAsync(_ => true).ConfigureAwait(false);
        var deserialized = cursor.ToList();
        return deserialized.ToImmutableList();
    }

    /// <inheritdoc/>
    public IObservable<IEnumerable<RecommendationState>> ObserveRecommendations()
    {
        throw new NotImplementedException();
    }

    FilterDefinition<RecommendationState> GetIdFilter(Guid id) => Builders<RecommendationState>.Filter.Eq(new StringFieldDefinition<RecommendationState, Guid>("_id"), id);
}
