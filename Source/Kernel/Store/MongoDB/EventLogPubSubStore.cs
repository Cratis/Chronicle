// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.MongoDB;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Orleans;
using Orleans.Runtime;
using Orleans.Serialization;
using Orleans.Storage;

namespace Aksio.Cratis.Events.Store.MongoDB;

/// <summary>
/// Represents the <see cref="IGrainStorage"/> for "PubSubStore" - which is used to store state about producers and consumers.
/// </summary>
public class EventLogPubSubStore : IGrainStorage
{
    const string CollectionName = "pub-sub-state";
    readonly JsonSerializerSettings _serializerSettings;
    readonly ISharedDatabase _database;
    readonly ILogger<EventLogPubSubStore> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventLogPubSubStore"/> class.
    /// </summary>
    /// <param name="database"><see cref="ISharedDatabase"/> to keep state in.</param>
    /// <param name="typeResolver"><see cref="ITypeResolver"/> to use for resolving types.</param>
    /// <param name="grainFactory"><see cref="IGrainFactory"/> for resolving grains during serialization.</param>
    /// <param name="logger">Logger for logging.</param>
    public EventLogPubSubStore(
        ISharedDatabase database,
        ITypeResolver typeResolver,
        IGrainFactory grainFactory,
        ILogger<EventLogPubSubStore> logger)
    {
        _serializerSettings = OrleansJsonSerializer.GetDefaultSerializerSettings(typeResolver, grainFactory);
        _database = database;
        _logger = logger;
    }

    /// <inheritdoc/>
    public Task ClearStateAsync(string grainType, GrainReference grainReference, IGrainState grainState)
    {
        _logger.ClearingState();
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task ReadStateAsync(string grainType, GrainReference grainReference, IGrainState grainState)
    {
        _logger.ReadingState();

        var json = JsonConvert.SerializeObject(grainState.State, _serializerSettings);
        json = json.Replace("\"$", "\"_", StringComparison.InvariantCulture);
        var jsonObject = JObject.Parse(json);
        var filter = new BsonDocument
        {
            ["_id"] = jsonObject["_id"]!.ToString()
        };

        var collection = _database.GetCollection<BsonDocument>(CollectionName);
        var result = await collection.FindAsync<BsonDocument>(filter);
        var document = result.SingleOrDefault();
        if (document != null)
        {
            json = document.ToJson();
            json = json.Replace("\"_", "\"$", StringComparison.InvariantCulture);
            grainState.State = JsonConvert.DeserializeObject(json, grainState.Type, _serializerSettings);
        }
    }

    /// <inheritdoc/>
    public async Task WriteStateAsync(string grainType, GrainReference grainReference, IGrainState grainState)
    {
        _logger.WritingState();

        var json = JsonConvert.SerializeObject(grainState.State, _serializerSettings);
        json = json.Replace("\"$", "\"_", StringComparison.InvariantCulture);
        var jsonObject = JObject.Parse(json);
        var filter = new BsonDocument
        {
            ["_id"] = jsonObject["_id"]!.ToString()
        };
        var document = BsonDocument.Parse(json);
        var collection = _database.GetCollection<BsonDocument>(CollectionName);
        await collection.ReplaceOneAsync(filter, document, new ReplaceOptions { IsUpsert = true });
    }
}
