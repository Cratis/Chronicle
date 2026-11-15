// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Webhooks;
using Cratis.Chronicle.Storage.MongoDB.EventSequences;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Observation.Webhooks.for_WebhookSigningKeysStorage.given;

public class a_signing_keys_storage(ReplicaSetMongoDBFixture fixture) : Specification
{
    protected WebhookSigningKeysStorage _storage;
    protected WebhookSigningKeysStorage _otherStorage;
    protected IEnumerable<Concepts.Observation.Webhooks.WebhookSigningKey> _keys;
    protected IEnumerable<Concepts.Observation.Webhooks.WebhookSigningKey> _otherKeys;
    protected IMongoDatabase _rawDatabase;
    MongoClient _client;
    string _databaseName;
    string _otherDatabaseName;

    protected static Concepts.Observation.Webhooks.WebhookSigningKey Active(string id = "active") => new(id, WebhookSigningAlgorithm.Ed25519, WebhookSigningKeyStatus.Active, "whpk_a", "enc:whsk_a", null, DateTimeOffset.FromUnixTimeSeconds(100), null, null);

    protected static Concepts.Observation.Webhooks.WebhookSigningKey OverlapForWebhook(string id = "overlap") => new(id, WebhookSigningAlgorithm.HmacSha256, WebhookSigningKeyStatus.Overlap, string.Empty, "enc:whsec_b", "orders", DateTimeOffset.FromUnixTimeSeconds(110), DateTimeOffset.FromUnixTimeSeconds(200), null);

    protected static Concepts.Observation.Webhooks.WebhookSigningKey Retired(string id = "retired") => new(id, WebhookSigningAlgorithm.Ed25519, WebhookSigningKeyStatus.Retired, "whpk_c", EncryptedWebhookSigningSecret.Empty, null, DateTimeOffset.FromUnixTimeSeconds(120), DateTimeOffset.FromUnixTimeSeconds(200), DateTimeOffset.FromUnixTimeSeconds(300));

    protected async Task Read()
    {
        _keys = await _storage.GetAll();
        _otherKeys = await _otherStorage.GetAll();
    }

    void Establish()
    {
        _client = new MongoClient(fixture.ConnectionString);
        _databaseName = MongoDBSpecDatabaseNames.New();
        _otherDatabaseName = MongoDBSpecDatabaseNames.New();
        _rawDatabase = _client.GetDatabase(_databaseName);
        _storage = new(DatabaseFor(_databaseName));
        _otherStorage = new(DatabaseFor(_otherDatabaseName));
    }

    async Task Destroy()
    {
        await _client.DropDatabaseAsync(_databaseName);
        await _client.DropDatabaseAsync(_otherDatabaseName);
    }

    IEventStoreDatabase DatabaseFor(string name)
    {
        var database = _client.GetDatabase(name);
        var eventStoreDatabase = Substitute.For<IEventStoreDatabase>();
        eventStoreDatabase.GetCollection<MongoDB.Observation.Webhooks.WebhookSigningKey>(WellKnownCollectionNames.WebhookSigningKeys)
            .Returns(database.GetCollection<MongoDB.Observation.Webhooks.WebhookSigningKey>(WellKnownCollectionNames.WebhookSigningKeys));
        return eventStoreDatabase;
    }
}
