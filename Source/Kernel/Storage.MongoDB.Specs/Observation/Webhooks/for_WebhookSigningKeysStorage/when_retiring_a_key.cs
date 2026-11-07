// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Webhooks;
using Cratis.Chronicle.Storage.MongoDB.EventSequences;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Observation.Webhooks.for_WebhookSigningKeysStorage;

[Collection(ReplicaSetMongoDBCollection.Name)]
public class when_retiring_a_key(ReplicaSetMongoDBFixture fixture) : given.a_signing_keys_storage(fixture)
{
    string _storedJson;

    async Task Because()
    {
        await _storage.Save(Active("key"));
        await _storage.Save(Retired("key"));
        await Read();
        var documents = await _rawDatabase.GetCollection<BsonDocument>(WellKnownCollectionNames.WebhookSigningKeys).Find(FilterDefinition<BsonDocument>.Empty).ToListAsync();
        _storedJson = string.Join(' ', documents.Select(_ => _.ToJson()));
    }

    [Fact] void should_keep_one_key() => _keys.Count().ShouldEqual(1);
    [Fact] void should_have_no_encrypted_secret() => _keys.Single().EncryptedSecret.Value.ShouldBeEmpty();
    [Fact] void should_be_retired() => _keys.Single().Status.ShouldEqual(WebhookSigningKeyStatus.Retired);
    [Fact] void should_not_leave_the_ciphertext_in_the_stored_document() => _storedJson.ShouldNotContain("enc:whsk_a");
}
