// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Compliance.GDPR.for_PIIManager.when_deleting_an_encryption_key;

/// <summary>
/// Both calls happening is not the property that matters - the order they happen in is. Evicting before the key is
/// durably gone tells every peer silo to drop it and then leaves them free to read it straight back out of a store that
/// still holds it, and re-cache it, which is the same window this decorator closes locally reopened at cluster scale.
/// The erase therefore has to be the thing that goes first, and the eviction the thing that follows it.
/// </summary>
public class and_the_durable_erase_succeeds : given.a_pii_manager
{
    Task Because() => _manager.DeleteEncryptionKeyFor(Identifier);

    [Fact] void should_erase_the_key_from_storage() => _keyStore.Received(1).DeleteFor(EventStore, EventStoreNamespace, Identifier);
    [Fact] void should_evict_the_key_from_every_silo() => _cacheClient.Received(1).Evict(EventStore, EventStoreNamespace, Identifier);
    [Fact] void should_erase_the_key_before_evicting_it() => Received.InOrder(() =>
    {
        _keyStore.DeleteFor(EventStore, EventStoreNamespace, Identifier);
        _cacheClient.Evict(EventStore, EventStoreNamespace, Identifier);
    });
}
