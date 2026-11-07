// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Webhooks.for_WebhookSigningKeysStorage;

public class when_reading_an_empty_sqlite_store : given.a_sqlite_signing_keys_storage
{
    async Task Because() => await Read();

    [Fact] void should_return_no_keys() => _keys.ShouldBeEmpty();
}
