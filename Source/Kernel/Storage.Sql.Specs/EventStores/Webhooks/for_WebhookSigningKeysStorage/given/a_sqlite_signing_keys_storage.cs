// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.for_Database.given;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Webhooks.for_WebhookSigningKeysStorage.given;

public class a_sqlite_signing_keys_storage : a_sqlite_database
{
    protected WebhookSigningKeysStorage _storage;
    protected WebhookSigningKeysStorage _otherStorage;
    protected IEnumerable<Concepts.Observation.Webhooks.WebhookSigningKey> _keys;
    protected IEnumerable<Concepts.Observation.Webhooks.WebhookSigningKey> _otherKeys;

    async Task Establish()
    {
        _storage = new("signing-keys-store", _database);
        _otherStorage = new("other-signing-keys-store", _database);
        await using var scope = await _database.EventStore("signing-keys-store");
        await using var otherScope = await _database.EventStore("other-signing-keys-store");
    }

    protected async Task Read()
    {
        _keys = await _storage.GetAll();
        _otherKeys = await _otherStorage.GetAll();
    }
}
