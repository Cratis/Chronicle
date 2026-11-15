// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB.EventSequences;

namespace Cratis.Chronicle.Storage.MongoDB.Observation.Webhooks.for_WebhookSigningKeysStorage;

[Collection(ReplicaSetMongoDBCollection.Name)]
public class when_reading_an_empty_store(ReplicaSetMongoDBFixture fixture) : given.a_signing_keys_storage(fixture)
{
    async Task Because() => await Read();

    [Fact] void should_return_no_keys() => _keys.ShouldBeEmpty();
}
