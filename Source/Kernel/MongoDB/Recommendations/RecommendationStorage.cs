// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Aksio.Cratis.Kernel.MongoDB.Observation;
using Aksio.Cratis.Kernel.Storage.Recommendations;
using Aksio.Cratis.Recommendations;
using Aksio.DependencyInversion;
using MongoDB.Driver;

namespace Aksio.Cratis.Kernel.MongoDB.Recommendations;

/// <summary>
/// Represents an implementation of <see cref="IRecommendationStorage"/> for MongoDB.
/// </summary>
public class RecommendationStorage : IRecommendationStorage
{
    readonly ProviderFor<IEventStoreInstanceDatabase> _databaseProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="RecommendationStorage"/> class.
    /// </summary>
    /// <param name="databaseProvider">Provider for <see cref="IEventStoreInstanceDatabase"/>.</param>
    public RecommendationStorage(
        ProviderFor<IEventStoreInstanceDatabase> databaseProvider)
    {
        _databaseProvider = databaseProvider;
    }

    IMongoCollection<RecommendationState> Collection => _databaseProvider().GetCollection<RecommendationState>(WellKnownCollectionNames.Recommendations);

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
    public async Task<IImmutableList<RecommendationState>> GetRecommendations()
    {
        var cursor = await Collection.FindAsync(_ => true).ConfigureAwait(false);
        var deserialized = cursor.ToList();
        return deserialized.ToImmutableList();
    }

    /// <inheritdoc/>
    public IObservable<IEnumerable<RecommendationState>> ObserveRecommendations()
    {
        var recommendations = GetRecommendations().GetAwaiter().GetResult();
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
