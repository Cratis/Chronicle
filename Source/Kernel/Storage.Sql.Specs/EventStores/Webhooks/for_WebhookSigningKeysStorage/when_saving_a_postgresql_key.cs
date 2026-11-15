// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.EventStores.Webhooks.for_WebhookSigningKeysStorage.given;
using Cratis.Chronicle.Storage.Sql.Sinks;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Webhooks.for_WebhookSigningKeysStorage;

[Collection(PostgreSqlCollection.Name)]
public class when_saving_a_postgresql_key(PostgreSqlFixture fixture) : given.a_postgresql_signing_keys_storage(fixture)
{
    async Task Because()
    {
        await _storage.Save(signing_keys.Active());
        await _storage.Save(signing_keys.OverlapForWebhook());
        await _storage.Save(signing_keys.Retired());
        await Read();
    }

    [Fact] void should_return_every_field_of_every_key() => _keys.ShouldContainOnly([signing_keys.Active(), signing_keys.OverlapForWebhook(), signing_keys.Retired()]);
}
