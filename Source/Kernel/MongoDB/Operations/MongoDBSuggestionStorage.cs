// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text.Json;
using Aksio.Cratis.Kernel.Grains.Suggestions;
using Aksio.Cratis.Kernel.MongoDB.Observation;
using Aksio.Cratis.Kernel.Persistence.Suggestions;
using Aksio.Cratis.Kernel.Suggestions;
using Aksio.DependencyInversion;
using Aksio.Strings;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace Aksio.Cratis.Kernel.MongoDB.Suggestions;

/// <summary>
/// Represents an implementation of <see cref="ISuggestionStorage"/> for MongoDB.
/// </summary>
public class MongoDBSuggestionStorage : ISuggestionStorage
{
    const string SuggestionRequestType = "requestType";
    readonly ProviderFor<IEventStoreDatabase> _databaseProvider;
    readonly JsonSerializerOptions _jsonSerializerOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="MongoDBSuggestionStorage"/> class.
    /// </summary>
    /// <param name="databaseProvider">Provider for <see cref="IEventStoreDatabase"/>.</param>
    /// <param name="jsonSerializerOptions"><see cref="JsonSerializerOptions"/> for JSON serialization.</param>
    public MongoDBSuggestionStorage(
        ProviderFor<IEventStoreDatabase> databaseProvider,
        JsonSerializerOptions jsonSerializerOptions)
    {
        _databaseProvider = databaseProvider;
        _jsonSerializerOptions = jsonSerializerOptions;
    }

    IMongoCollection<BsonDocument> Collection => _databaseProvider().GetCollection<BsonDocument>(WellKnownCollectionNames.Suggestions);

    /// <inheritdoc/>
    public async Task<SuggestionState?> Get(SuggestionId suggestionId)
    {
        var filter = GetIdFilter(suggestionId);
        var cursor = await Collection.FindAsync(filter).ConfigureAwait(false);
        var state = cursor.SingleOrDefault();
        if (state is null) return null;
        return DeserializeState(state);
    }

    /// <inheritdoc/>
    public Task Remove(SuggestionId suggestionId)
    {
        var filter = GetIdFilter(suggestionId);
        return Collection.DeleteOneAsync(filter);
    }

    /// <inheritdoc/>
    public Task Save(SuggestionId suggestionId, SuggestionState suggestionState)
    {
        var filter = GetIdFilter(suggestionId);
        var document = suggestionState.ToBsonDocument();
        var requestProperty = nameof(SuggestionState.Request).ToCamelCase();
        document.Remove("_id");
        document[SuggestionRequestType] = suggestionState.Request.GetType().AssemblyQualifiedName;
        var json = JsonSerializer.Serialize(suggestionState.Request, _jsonSerializerOptions);
        document[requestProperty] = BsonDocument.Parse(json);
        return Collection.ReplaceOneAsync(filter, document, new ReplaceOptions { IsUpsert = true });
    }

    /// <inheritdoc/>
    public async Task<IImmutableList<SuggestionState>> GetSuggestions()
    {
        var cursor = await Collection.FindAsync(_ => true).ConfigureAwait(false);
        var deserialized = cursor.ToList().Select(DeserializeState);
        return deserialized.ToImmutableList();
    }

    /// <inheritdoc/>
    public IObservable<IEnumerable<SuggestionState>> ObserveSuggestions()
    {
        var suggestions = GetSuggestions().GetAwaiter().GetResult();
        return Collection.Observe(suggestions, HandleChangesForSuggestions);
    }

    SuggestionState DeserializeState(BsonDocument document)
    {
        var requestProperty = nameof(SuggestionState.Request).ToCamelCase();
        var requestAsDocument = document.GetValue(requestProperty).AsBsonDocument;
        var requestType = Type.GetType(document[SuggestionRequestType].AsString);
        var request = BsonSerializer.Deserialize(requestAsDocument, requestType);
        document.Remove(requestProperty);
        var state = BsonSerializer.Deserialize<SuggestionState>(document);
        state.Request = request;
        return state;
    }

    void HandleChangesForSuggestions(IChangeStreamCursor<ChangeStreamDocument<BsonDocument>> cursor, List<SuggestionState> suggestions)
    {
        foreach (var change in cursor.Current)
        {
            var changedSuggestion = change.FullDocument;
            if (change.OperationType == ChangeStreamOperationType.Delete)
            {
                var suggestion = suggestions.Find(_ => _.Id == (SuggestionId)change.DocumentKey["_id"].AsGuid);
                if (suggestion is not null)
                {
                    suggestions.Remove(suggestion);
                }
                continue;
            }

            var observer = suggestions.Find(_ => _.Id == (SuggestionId)changedSuggestion["_id"].AsGuid);

            var suggestionState = DeserializeState(changedSuggestion);
            if (observer is not null)
            {
                var index = suggestions.IndexOf(observer);
                suggestions[index] = suggestionState;
            }
            else
            {
                suggestions.Add(suggestionState);
            }
        }
    }

    FilterDefinition<BsonDocument> GetIdFilter(Guid id) => Builders<BsonDocument>.Filter.Eq(new StringFieldDefinition<BsonDocument, Guid>("_id"), id);
}
