// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Webhooks;
using Cratis.Chronicle.Storage.MongoDB.EventSequences;

namespace Cratis.Chronicle.Storage.MongoDB.Observation.Webhooks.for_WebhookSigningKeysStorage;

[Collection(ReplicaSetMongoDBCollection.Name)]
public class when_saving_the_same_key_twice(ReplicaSetMongoDBFixture fixture) : given.a_signing_keys_storage(fixture)
{
    async Task Because()
    {
        await _storage.Save(Active());
        await _storage.Save(Active() with { Status = WebhookSigningKeyStatus.Overlap, OverlapEndsAt = DateTimeOffset.FromUnixTimeSeconds(500) });
        await Read();
    }

    [Fact] void should_hold_one_key() => _keys.Count().ShouldEqual(1);
    [Fact] void should_hold_the_latest_values() => _keys.Single().ShouldEqual(Active() with { Status = WebhookSigningKeyStatus.Overlap, OverlapEndsAt = DateTimeOffset.FromUnixTimeSeconds(500) });
}
