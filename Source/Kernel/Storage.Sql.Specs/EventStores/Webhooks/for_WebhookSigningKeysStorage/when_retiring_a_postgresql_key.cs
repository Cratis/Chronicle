// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Webhooks;
using Cratis.Chronicle.Storage.Sql.EventStores.Webhooks.for_WebhookSigningKeysStorage.given;
using Cratis.Chronicle.Storage.Sql.Sinks;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Webhooks.for_WebhookSigningKeysStorage;

[Collection(PostgreSqlCollection.Name)]
public class when_retiring_a_postgresql_key(PostgreSqlFixture fixture) : given.a_postgresql_signing_keys_storage(fixture)
{
    async Task Because()
    {
        await _storage.Save(signing_keys.Active("key"));
        await _storage.Save(signing_keys.Retired("key"));
        await Read();
    }

    [Fact] void should_keep_one_key() => _keys.Count().ShouldEqual(1);
    [Fact] void should_have_no_encrypted_secret() => _keys.Single().EncryptedSecret.Value.ShouldBeEmpty();
    [Fact] void should_be_retired() => _keys.Single().Status.ShouldEqual(WebhookSigningKeyStatus.Retired);
}
