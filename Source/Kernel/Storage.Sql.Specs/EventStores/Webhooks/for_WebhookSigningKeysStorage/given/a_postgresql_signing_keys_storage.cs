// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.for_Database.given;
using Cratis.Chronicle.Storage.Sql.Sinks;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Webhooks.for_WebhookSigningKeysStorage.given;

public class a_postgresql_signing_keys_storage(PostgreSqlFixture fixture) : a_postgresql_database(fixture)
{
    protected WebhookSigningKeysStorage _storage;
    protected WebhookSigningKeysStorage _otherStorage;
    protected IEnumerable<Concepts.Observation.Webhooks.WebhookSigningKey> _keys;
    protected IEnumerable<Concepts.Observation.Webhooks.WebhookSigningKey> _otherKeys;

    async Task Establish()
    {
        _storage = new(Store, Database);
        _otherStorage = new("another-store", Database);
        await using var scope = await Database.EventStore(Store);
        await using var otherScope = await Database.EventStore("another-store");
    }

    protected async Task Read()
    {
        _keys = await _storage.GetAll();
        _otherKeys = await _otherStorage.GetAll();
    }
}
