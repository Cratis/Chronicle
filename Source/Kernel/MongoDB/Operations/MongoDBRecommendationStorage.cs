// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text.Json;
using Aksio.Cratis.Kernel.Grains.Recommendations;
using Aksio.Cratis.Kernel.MongoDB.Observation;
using Aksio.Cratis.Kernel.Persistence.Recommendations;
using Aksio.Cratis.Kernel.Recommendations;
using Aksio.DependencyInversion;
using Aksio.Strings;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace Aksio.Cratis.Kernel.MongoDB.Recommendations;

/// <summary>
/// Represents an implementation of <see cref="IRecommendationStorage"/> for MongoDB.
/// </summary>
public class MongoDBRecommendationStorage : IRecommendationStorage
{
    const string RecommendationRequestType = "requestType";
    readonly ProviderFor<IEventStoreDatabase> _databaseProvider;
    readonly JsonSerializerOptions _jsonSerializerOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="MongoDBRecommendationStorage"/> class.
    /// </summary>
    /// <param name="databaseProvider">Provider for <see cref="IEventStoreDatabase"/>.</param>
    /// <param name="jsonSerializerOptions"><see cref="JsonSerializerOptions"/> for JSON serialization.</param>
    public MongoDBRecommendationStorage(
        ProviderFor<IEventStoreDatabase> databaseProvider,
        JsonSerializerOptions jsonSerializerOptions)
    {
        _databaseProvider = databaseProvider;
        _jsonSerializerOptions = jsonSerializerOptions;
    }

    IMongoCollection<BsonDocument> Collection => _databaseProvider().GetCollection<BsonDocument>(WellKnownCollectionNames.Recommendations);

    /// <inheritdoc/>
    public async Task<RecommendationState?> Get(RecommendationId recommendationId)
    {
        var filter = GetIdFilter(recommendationId);
        var cursor = await Collection.FindAsync(filter).ConfigureAwait(false);
        var state = cursor.SingleOrDefault();
        if (state is null) return null;
        return DeserializeState(state);
    }

    /// <inheritdoc/>
    public Task Remove(RecommendationId recommendationId)
    {
        var filter = GetIdFilter(recommendationId);
        return Collection.DeleteOneAsync(filter);
    }

    /// <inheritdoc/>
    public Task Save(RecommendationId recommendationId, RecommendationState recommendationState)
    {
        var filter = GetIdFilter(recommendationId);
        var document = recommendationState.ToBsonDocument();
        var requestProperty = nameof(RecommendationState.Request).ToCamelCase();
        document.Remove("_id");
        document[RecommendationRequestType] = recommendationState.Request.GetType().AssemblyQualifiedName;
        var json = JsonSerializer.Serialize(recommendationState.Request, _jsonSerializerOptions);
        document[requestProperty] = BsonDocument.Parse(json);
        return Collection.ReplaceOneAsync(filter, document, new ReplaceOptions { IsUpsert = true });
    }

    /// <inheritdoc/>
    public async Task<IImmutableList<RecommendationState>> GetRecommendations()
    {
        var cursor = await Collection.FindAsync(_ => true).ConfigureAwait(false);
        var deserialized = cursor.ToList().Select(DeserializeState);
        return deserialized.ToImmutableList();
    }

    /// <inheritdoc/>
    public IObservable<IEnumerable<RecommendationState>> ObserveRecommendations()
    {
        var recommendations = GetRecommendations().GetAwaiter().GetResult();
        return Collection.Observe(recommendations, HandleChangesForRecommendations);
    }

    RecommendationState DeserializeState(BsonDocument document)
    {
        var requestProperty = nameof(RecommendationState.Request).ToCamelCase();
        var requestAsDocument = document.GetValue(requestProperty).AsBsonDocument;
        var requestType = Type.GetType(document[RecommendationRequestType].AsString);
        var request = BsonSerializer.Deserialize(requestAsDocument, requestType);
        document.Remove(requestProperty);
        var state = BsonSerializer.Deserialize<RecommendationState>(document);
        state.Request = request;
        return state;
    }

    void HandleChangesForRecommendations(IChangeStreamCursor<ChangeStreamDocument<BsonDocument>> cursor, List<RecommendationState> recommendations)
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

            var observer = recommendations.Find(_ => _.Id == (RecommendationId)changedRecommendation["_id"].AsGuid);

            var recommendationState = DeserializeState(changedRecommendation);
            if (observer is not null)
            {
                var index = recommendations.IndexOf(observer);
                recommendations[index] = recommendationState;
            }
            else
            {
                recommendations.Add(recommendationState);
            }
        }
    }

    FilterDefinition<BsonDocument> GetIdFilter(Guid id) => Builders<BsonDocument>.Filter.Eq(new StringFieldDefinition<BsonDocument, Guid>("_id"), id);
}
