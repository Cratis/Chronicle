// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text.Json;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Observation.Registrations;
using Cratis.Chronicle.Storage.Observation.Registrations;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Observation.Registrations;

/// <summary>
/// Persists registrations and independent owner and generation versions using atomic document writes.
/// </summary>
/// <param name="database">The event-store database.</param>
/// <param name="serializerOptions">Options for serializing complete definitions.</param>
public class ObserverRegistrationsStorage(IEventStoreDatabase database, JsonSerializerOptions serializerOptions) : IObserverRegistrationsStorage
{
    readonly Lazy<Task> _indexes = new(() => CreateIndexes(database));

    IMongoCollection<BsonDocument> Registrations => database.GetCollection<BsonDocument>(WellKnownCollectionNames.ObserverRegistrations);
    IMongoCollection<BsonDocument> Settings => database.GetCollection<BsonDocument>(WellKnownCollectionNames.ObserverOwnerSettings);
    IMongoCollection<BsonDocument> Floor => database.GetCollection<BsonDocument>(WellKnownCollectionNames.ObserverGenerationFloor);

    /// <inheritdoc/>
    public Task<ObserverRegistration?> Get(ObserverId observerId, EventSequenceId eventSequenceId) => Read<ObserverRegistration>(Registrations, Identity(observerId, eventSequenceId));

    /// <inheritdoc/>
    public Task<IEnumerable<ObserverRegistration>> GetActive() => Find(Builders<BsonDocument>.Filter.Eq("active", true));

    /// <inheritdoc/>
    public Task<IEnumerable<ObserverRegistration>> GetByOwner(ProgramIdentifier owner) => Find(Builders<BsonDocument>.Filter.Eq("owner", owner.Value));

    /// <inheritdoc/>
    public async Task<bool> TrySave(ObserverRegistration registration, ObserverRegistrationRevision expectedRevision)
    {
        if (!IsNext(registration.Revision.Value, expectedRevision.Value))
        {
            return false;
        }

        await _indexes.Value;
        var document = Document(Identity(registration.ObserverId, registration.EventSequenceId), registration.Revision.Value, registration);
        document["observerId"] = registration.ObserverId.Value;
        document["eventSequenceId"] = registration.EventSequenceId.Value;
        document["active"] = registration.IsActive;
        document["owner"] = registration.Owner.Value;
        return await Write(Registrations, document, expectedRevision.Value);
    }

    /// <inheritdoc/>
    public async Task<bool> TryDelete(ObserverId observerId, EventSequenceId eventSequenceId, ObserverRegistrationRevision expectedRevision) =>
        (await Registrations.DeleteOneAsync(Builders<BsonDocument>.Filter.Eq("_id", Identity(observerId, eventSequenceId)) & Builders<BsonDocument>.Filter.Eq("revision", Number(expectedRevision.Value)))).DeletedCount == 1;

    /// <inheritdoc/>
    public Task<ObserverGenerationFloor?> GetGenerationFloor() => Read<ObserverGenerationFloor>(Floor, new BsonString("floor"));

    /// <inheritdoc/>
    public Task<bool> TrySaveGenerationFloor(ObserverGenerationFloor floor, ulong expectedVersion)
    {
        if (!IsNext(floor.Version, expectedVersion))
        {
            return Task.FromResult(false);
        }

        var document = Document(new BsonString("floor"), floor.Version, floor);
        document["generation"] = Number(floor.Generation);
        return Write(Floor, document, expectedVersion, Builders<BsonDocument>.Filter.Lte("generation", Number(floor.Generation)));
    }

    /// <inheritdoc/>
    public Task<ObserverOwnerSettings?> GetOwnerSettings(ProgramIdentifier owner) => Read<ObserverOwnerSettings>(Settings, new BsonString(owner.Value));

    /// <inheritdoc/>
    public Task<bool> TrySaveOwnerSettings(ObserverOwnerSettings settings, ulong expectedRevision) =>
        settings.HasValidTimeout && IsNext(settings.Revision, expectedRevision)
            ? Write(Settings, Document(new BsonString(settings.Owner.Value), settings.Revision, settings), expectedRevision)
            : Task.FromResult(false);

    static Task CreateIndexes(IEventStoreDatabase database) => database.GetCollection<BsonDocument>(WellKnownCollectionNames.ObserverRegistrations).Indexes.CreateOneAsync(
        new CreateIndexModel<BsonDocument>(
            Builders<BsonDocument>.IndexKeys.Ascending("observerId").Ascending("eventSequenceId"),
            new CreateIndexOptions { Unique = true, Name = "observer-sequence" }));

    static bool IsNext(ulong revision, ulong expected) => expected < ulong.MaxValue && revision == expected + 1;

    static string Number(ulong number) => number.ToString("D20", CultureInfo.InvariantCulture);

    static BsonDocument Identity(ObserverId observerId, EventSequenceId eventSequenceId) => new() { ["observer"] = observerId.Value, ["sequence"] = eventSequenceId.Value };

    BsonDocument Document<T>(BsonValue id, ulong revision, T value) => new()
    {
        ["_id"] = id,
        ["revision"] = Number(revision),
        ["data"] = JsonSerializer.Serialize(value, serializerOptions)
    };

    async Task<T?> Read<T>(IMongoCollection<BsonDocument> collection, BsonValue id)
    {
        var document = await collection.Find(Builders<BsonDocument>.Filter.Eq("_id", id)).SingleOrDefaultAsync();
        return document is null ? default : JsonSerializer.Deserialize<T>(document["data"].AsString, serializerOptions);
    }

    async Task<IEnumerable<ObserverRegistration>> Find(FilterDefinition<BsonDocument> filter)
    {
        var documents = await Registrations.Find(filter).ToListAsync();
        return documents.Select(document => JsonSerializer.Deserialize<ObserverRegistration>(document["data"].AsString, serializerOptions)!).ToArray();
    }

    static async Task<bool> Write(IMongoCollection<BsonDocument> collection, BsonDocument document, ulong expectedRevision, FilterDefinition<BsonDocument>? additionalFilter = null)
    {
        if (expectedRevision > 0)
        {
            var filter = Builders<BsonDocument>.Filter.Eq("_id", document["_id"]) & Builders<BsonDocument>.Filter.Eq("revision", Number(expectedRevision));
            return (await collection.ReplaceOneAsync(filter & (additionalFilter ?? FilterDefinition<BsonDocument>.Empty), document)).ModifiedCount == 1;
        }

        try
        {
            await collection.InsertOneAsync(document);
            return true;
        }
        catch (MongoWriteException exception) when (exception.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            return false;
        }
    }
}
