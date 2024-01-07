// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.MongoDB;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json;
using Orleans.Configuration;
using Orleans.Runtime;
using Orleans.Serialization;

namespace Aksio.Cratis.Kernel.Storage.MongoDB.Reminders;

/// <summary>
/// Represents an implementation of <see cref="IReminderTable"/>.
/// </summary>
public class ReminderTable : IReminderTable
{
    const string ETagProperty = "eTag";
    const string GrainKeyProperty = "grainKey";
    const string GrainHashProperty = "grainHash";
    const string ServiceIdProperty = "serviceId";

    readonly JsonSerializerSettings _serializerSettings;
    readonly IDatabase _database;
    readonly IOptions<ClusterOptions> _clusterOptions;
    readonly ILogger<ReminderTable> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReminderTable"/> class.
    /// </summary>
    /// <param name="database"><see cref="IDatabase"/> to keep state in.</param>
    /// <param name="clusterOptions">The <see cref="ClusterOptions"/>.</param>
    /// <param name="orleansJsonSerializerOptions">The Orleans Json serializer options.</param>
    /// <param name="logger">Logger for logging.</param>
    public ReminderTable(
        IDatabase database,
        IOptions<ClusterOptions> clusterOptions,
        IOptions<OrleansJsonSerializerOptions> orleansJsonSerializerOptions,
        ILogger<ReminderTable> logger)
    {
        _serializerSettings = orleansJsonSerializerOptions.Value.JsonSerializerSettings;
        _database = database;
        _clusterOptions = clusterOptions;
        _logger = logger;
    }

    /// <inheritdoc/>
    public Task Init() => Task.CompletedTask;

    /// <inheritdoc/>
    public async Task<ReminderEntry> ReadRow(GrainId grainId, string reminderName)
    {
        var key = GetKeyFrom(grainId, reminderName);
        var filter = GetKeyFilterFor(key);

        _logger.ReadingSpecificReminderForGrain(grainId.GetGuidKey().ToString(), reminderName);

        var result = await GetCollection().FindAsync(filter);
        var entries = result.ToList();
        if (entries.Count != 1) return null!;
        return entries.Select(Deserialize).Single()!;
    }

    /// <inheritdoc/>
    public async Task<ReminderTableData> ReadRows(GrainId grainId)
    {
        _logger.ReadingAllRemindersForGrain(grainId.GetGuidKey().ToString());

        var filter = Builders<BsonDocument>.Filter.And(
            Builders<BsonDocument>.Filter.Eq(new StringFieldDefinition<BsonDocument, string>(ServiceIdProperty), _clusterOptions.Value.ServiceId),
            Builders<BsonDocument>.Filter.Eq(new StringFieldDefinition<BsonDocument, string>(GrainKeyProperty), grainId.GetGuidKey().ToString()));

        var result = await GetCollection().FindAsync(filter);
        var entries = result.ToList().Select(Deserialize);
        return new ReminderTableData(entries);
    }

    /// <inheritdoc/>
    public async Task<ReminderTableData> ReadRows(uint begin, uint end)
    {
        _logger.ReadingRemindersInRange(begin, end);

        var filter = Builders<BsonDocument>.Filter.And(
            Builders<BsonDocument>.Filter.Eq(new StringFieldDefinition<BsonDocument, string>(ServiceIdProperty), _clusterOptions.Value.ServiceId),
            Builders<BsonDocument>.Filter.Gt(new StringFieldDefinition<BsonDocument, long>(GrainHashProperty), begin),
            Builders<BsonDocument>.Filter.Lte(new StringFieldDefinition<BsonDocument, long>(GrainHashProperty), end));

        var result = await GetCollection().FindAsync(filter);
        var entries = result.ToList().Select(Deserialize);
        return new ReminderTableData(entries);
    }

    /// <inheritdoc/>
    public async Task<bool> RemoveRow(GrainId grainId, string reminderName, string eTag)
    {
        var key = GetKeyFrom(grainId, reminderName);
        try
        {
            var filter = Builders<BsonDocument>.Filter.And(
                Builders<BsonDocument>.Filter.Eq(new StringFieldDefinition<BsonDocument, string>(ServiceIdProperty), _clusterOptions.Value.ServiceId),
                GetKeyFilterFor(key),
                Builders<BsonDocument>.Filter.Eq(new StringFieldDefinition<BsonDocument, string>(ETagProperty), eTag));
            _logger.Removing(key);

            var result = await GetCollection().DeleteOneAsync(filter);
            return result.DeletedCount == 1;
        }
        catch (Exception ex)
        {
            _logger.FailedRemoving(key, ex);
            return false;
        }
    }

    /// <inheritdoc/>
    public Task TestOnlyClearTable() => GetCollection().DeleteManyAsync(FilterDefinition<BsonDocument>.Empty);

    /// <inheritdoc/>
    public async Task<string> UpsertRow(ReminderEntry entry)
    {
        var key = GetKeyFrom(entry.GrainId, entry.ReminderName);
        try
        {
            var json = JsonConvert.SerializeObject(entry, _serializerSettings);
            json = json.Replace("\"$", "\"__");
            var filter = GetKeyFilterFor(key);
            var bson = BsonDocument.Parse(json);
            var hash = (long)entry.GrainId.GetUniformHashCode();
            bson[ETagProperty] = "1"; // TODO: What do we do with ETag ??
            bson[GrainKeyProperty] = entry.GrainId.GetGuidKey().ToString();
            bson[GrainHashProperty] = hash;
            bson[ServiceIdProperty] = _clusterOptions.Value.ServiceId;

            _logger.Upserting(key);

            await GetCollection().ReplaceOneAsync(filter, bson, new ReplaceOptions { IsUpsert = true });
            return "Reminder successfully upserted";
        }
        catch (Exception ex)
        {
            _logger.FailedUpserting(key, ex);
            return null!;
        }
    }

    ReminderEntry Deserialize(BsonDocument document)
    {
        document.Remove(GrainHashProperty);
        var json = document.ToJson();
        json = json.Replace("\"__", "\"$");
        return JsonConvert.DeserializeObject<ReminderEntry>(json, _serializerSettings)!;
    }

    IMongoCollection<BsonDocument> GetCollection() => _database.GetCollection<BsonDocument>(WellKnownCollectionNames.Reminders);

    FilterDefinition<BsonDocument> GetKeyFilterFor(string key) => Builders<BsonDocument>.Filter.Eq(new StringFieldDefinition<BsonDocument, string>("_id"), key);

    string GetKeyFrom(GrainId grainId, string reminderName) => $"{grainId.GetGuidKey()} : {reminderName}";
}
