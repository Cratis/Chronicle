// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Webhooks;
using Cratis.Chronicle.Storage.Sql.EventStores.Webhooks.for_WebhookSigningKeysStorage.given;
using Cratis.Chronicle.Storage.Sql.Sinks;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Webhooks.for_WebhookSigningKeysStorage;

[Collection(PostgreSqlCollection.Name)]
public class when_saving_the_same_postgresql_key_twice(PostgreSqlFixture fixture) : given.a_postgresql_signing_keys_storage(fixture)
{
    async Task Because()
    {
        await _storage.Save(signing_keys.Active());
        await _storage.Save(signing_keys.Active() with { Status = WebhookSigningKeyStatus.Overlap, OverlapEndsAt = DateTimeOffset.FromUnixTimeSeconds(500) });
        await Read();
    }

    [Fact] void should_hold_one_key() => _keys.Count().ShouldEqual(1);
    [Fact] void should_hold_the_latest_values() => _keys.Single().ShouldEqual(signing_keys.Active() with { Status = WebhookSigningKeyStatus.Overlap, OverlapEndsAt = DateTimeOffset.FromUnixTimeSeconds(500) });
}
