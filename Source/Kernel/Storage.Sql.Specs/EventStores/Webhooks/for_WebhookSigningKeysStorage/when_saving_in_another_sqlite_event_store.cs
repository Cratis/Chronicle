// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.EventStores.Webhooks.for_WebhookSigningKeysStorage.given;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Webhooks.for_WebhookSigningKeysStorage;

public class when_saving_in_another_sqlite_event_store : given.a_sqlite_signing_keys_storage
{
    async Task Because()
    {
        await _storage.Save(signing_keys.Active());
        await Read();
    }

    [Fact] void should_hold_the_key_in_its_own_store() => _keys.Count().ShouldEqual(1);
    [Fact] void should_not_leak_the_key_into_the_other_store() => _otherKeys.ShouldBeEmpty();
}
