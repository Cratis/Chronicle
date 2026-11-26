// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Registrations;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Registrations;

/// <summary>
/// Stores registration intents in the event-store database, one document per kind and key.
/// </summary>
/// <param name="database">The event-store database.</param>
public class RegistrationIntentStorage(IMongoDatabase database) : IRegistrationIntentStorage
{
    readonly IMongoCollection<BsonDocument> _intents = database.GetCollection<BsonDocument>(WellKnownCollectionNames.RegistrationIntents);

    /// <inheritdoc/>
    public async Task<IReadOnlyList<RegistrationIntent>> GetAll()
    {
        var documents = await _intents.Find(FilterDefinition<BsonDocument>.Empty).ToListAsync();
        return [.. documents.Select(FromDocument)];
    }

    /// <inheritdoc/>
    public async Task Save(RegistrationIntent intent)
    {
        var document = new BsonDocument
        {
            { "_id", $"{(int)intent.Kind}:{intent.Key}" },
            { "kind", (int)intent.Kind },
            { "key", intent.Key },
            { "registrant", intent.Registrant },
            { "revision", intent.Revision },
            { "hash", intent.Hash },
            { "state", (int)intent.State },
            { "payload", new BsonBinaryData(intent.Payload.ToArray()) },
            { "fullSetOwner", intent.FullSetOwner.HasValue ? new BsonInt32(intent.FullSetOwner.Value) : BsonNull.Value },
            { "version", ToBson(intent.Version) },
            { "attempts", intent.Attempts },
            { "failure", ToBson(intent.Failure) },
            { "updatedAt", intent.UpdatedAt.UtcDateTime }
        };
        await _intents.ReplaceOneAsync(new BsonDocument("_id", document["_id"]), document, new ReplaceOptions { IsUpsert = true });
    }

    static BsonValue ToBson(string? value) => value is null ? BsonNull.Value : new BsonString(value);

    static RegistrationIntent FromDocument(BsonDocument document) => new(
        (RegistrationKind)document["kind"].AsInt32,
        document["key"].AsString,
        document["registrant"].AsString,
        document["revision"].ToInt64(),
        document["hash"].AsString,
        (RegistrationIntentState)document["state"].AsInt32,
        document["payload"].AsBsonBinaryData.Bytes,
        document["fullSetOwner"].IsBsonNull ? null : document["fullSetOwner"].AsInt32,
        document["version"].IsBsonNull ? null : document["version"].AsString,
        document["attempts"].AsInt32,
        document["failure"].IsBsonNull ? null : document["failure"].AsString,
        new DateTimeOffset(DateTime.SpecifyKind(document["updatedAt"].ToUniversalTime(), DateTimeKind.Utc)));
}
