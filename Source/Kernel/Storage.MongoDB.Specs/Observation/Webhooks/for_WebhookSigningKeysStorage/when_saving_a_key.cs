// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB.EventSequences;

namespace Cratis.Chronicle.Storage.MongoDB.Observation.Webhooks.for_WebhookSigningKeysStorage;

[Collection(ReplicaSetMongoDBCollection.Name)]
public class when_saving_a_key(ReplicaSetMongoDBFixture fixture) : given.a_signing_keys_storage(fixture)
{
    async Task Because()
    {
        await _storage.Save(Active());
        await _storage.Save(OverlapForWebhook());
        await _storage.Save(Retired());
        await Read();
    }

    [Fact] void should_return_every_field_of_every_key() => _keys.ShouldContainOnly([Active(), OverlapForWebhook(), Retired()]);
}
