// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Concepts.Recommendations;
using Cratis.Chronicle.Storage.MongoDB.Observation;
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
        var recommendations = GeAll().GetAwaiter().GetResult();
        return Collection.Observe(recommendations, HandleChangesForRecommendations);
    }

    void HandleChangesForRecommendations(IChangeStreamCursor<ChangeStreamDocument<RecommendationState>> cursor, List<RecommendationState> recommendations)
    {
        foreach (var change in cursor.Current)
        {
            var changedRecommendation = change.FullDocument;
            if (change.OperationType == ChangeStreamOperationType.Delete)
            {
                var recommendation = recommendations.Find(_ => _.Id == (RecommendationId)change.DocumentKey["_id"].AsGuid);
                if (recommendation is not null)
                {
                    recommendations.Remove(recommendation);
                }
                continue;
            }

            var observer = recommendations.Find(_ => _.Id == changedRecommendation.Id);
            if (observer is not null)
            {
                var index = recommendations.IndexOf(observer);
                recommendations[index] = changedRecommendation;
            }
            else
            {
                recommendations.Add(changedRecommendation);
            }
        }
    }

    FilterDefinition<RecommendationState> GetIdFilter(Guid id) => Builders<RecommendationState>.Filter.Eq(new StringFieldDefinition<RecommendationState, Guid>("_id"), id);
}
