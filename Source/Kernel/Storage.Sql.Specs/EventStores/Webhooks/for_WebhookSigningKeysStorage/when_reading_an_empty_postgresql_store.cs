// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.Sinks;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Webhooks.for_WebhookSigningKeysStorage;

[Collection(PostgreSqlCollection.Name)]
public class when_reading_an_empty_postgresql_store(PostgreSqlFixture fixture) : given.a_postgresql_signing_keys_storage(fixture)
{
    async Task Because() => await Read();

    [Fact] void should_return_no_keys() => _keys.ShouldBeEmpty();
}
