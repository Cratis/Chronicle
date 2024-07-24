// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json;
using Orleans.Configuration;
using Orleans.Serialization;

namespace Cratis.Chronicle.Storage.MongoDB.Reminders;

/// <summary>
/// Represents an implementation of <see cref="IReminderTable"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ReminderTable"/> class.
/// </remarks>
/// <param name="database"><see cref="IDatabase"/> to keep state in.</param>
/// <param name="clusterOptions">The <see cref="ClusterOptions"/>.</param>
/// <param name="orleansJsonSerializerOptions">The Orleans Json serializer options.</param>
/// <param name="logger">Logger for logging.</param>
public class ReminderTable(
    IDatabase database,
    IOptions<ClusterOptions> clusterOptions,
    IOptions<OrleansJsonSerializerOptions> orleansJsonSerializerOptions,
    ILogger<ReminderTable> logger) : IReminderTable
{
    const string ETagProperty = "eTag";
    const string GrainKeyProperty = "grainKey";
    const string GrainHashProperty = "grainHash";
    const string ServiceIdProperty = "serviceId";

    readonly JsonSerializerSettings _serializerSettings = orleansJsonSerializerOptions.Value.JsonSerializerSettings;

    /// <inheritdoc/>
    public Task Init() => Task.CompletedTask;

    /// <inheritdoc/>
    public async Task<ReminderEntry> ReadRow(GrainId grainId, string reminderName)
    {
        var key = GetKeyFrom(grainId, reminderName);
        var filter = GetKeyFilterFor(key);

        logger.ReadingSpecificReminderForGrain(grainId.Key.ToString()!, reminderName);

        var result = await GetCollection().FindAsync(filter);
        var entries = result.ToList();
        if (entries.Count != 1) return null!;
        return entries.Select(Deserialize).Single()!;
    }

    /// <inheritdoc/>
    public async Task<ReminderTableData> ReadRows(GrainId grainId)
    {
        var key = grainId.Key.ToString()!;
        logger.ReadingAllRemindersForGrain(key);

        var filter = Builders<BsonDocument>.Filter.And(
            Builders<BsonDocument>.Filter.Eq(new StringFieldDefinition<BsonDocument, string>(ServiceIdProperty), clusterOptions.Value.ServiceId),
            Builders<BsonDocument>.Filter.Eq(new StringFieldDefinition<BsonDocument, string>(GrainKeyProperty), key));

        var result = await GetCollection().FindAsync(filter);
        var entries = result.ToList().Select(Deserialize);
        return new ReminderTableData(entries);
    }

    /// <inheritdoc/>
    public async Task<ReminderTableData> ReadRows(uint begin, uint end)
    {
        logger.ReadingRemindersInRange(begin, end);

        var filter = Builders<BsonDocument>.Filter.And(
            Builders<BsonDocument>.Filter.Eq(new StringFieldDefinition<BsonDocument, string>(ServiceIdProperty), clusterOptions.Value.ServiceId),
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
                Builders<BsonDocument>.Filter.Eq(new StringFieldDefinition<BsonDocument, string>(ServiceIdProperty), clusterOptions.Value.ServiceId),
                GetKeyFilterFor(key),
                Builders<BsonDocument>.Filter.Eq(new StringFieldDefinition<BsonDocument, string>(ETagProperty), eTag));
            logger.Removing(key);

            var result = await GetCollection().DeleteOneAsync(filter);
            return result.DeletedCount == 1;
        }
        catch (Exception ex)
        {
            logger.FailedRemoving(key, ex);
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
            bson[GrainKeyProperty] = entry.GrainId.Key.ToString();
            bson[GrainHashProperty] = hash;
            bson[ServiceIdProperty] = clusterOptions.Value.ServiceId;

            logger.Upserting(key);

            await GetCollection().ReplaceOneAsync(filter, bson, new ReplaceOptions { IsUpsert = true });
            return "Reminder successfully upserted";
        }
        catch (Exception ex)
        {
            logger.FailedUpserting(key, ex);
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

    IMongoCollection<BsonDocument> GetCollection() => database.GetCollection<BsonDocument>(WellKnownCollectionNames.Reminders);

    FilterDefinition<BsonDocument> GetKeyFilterFor(string key) => Builders<BsonDocument>.Filter.Eq(new StringFieldDefinition<BsonDocument, string>("_id"), key);

    string GetKeyFrom(GrainId grainId, string reminderName) => $"{grainId.Key} : {reminderName}";
}
