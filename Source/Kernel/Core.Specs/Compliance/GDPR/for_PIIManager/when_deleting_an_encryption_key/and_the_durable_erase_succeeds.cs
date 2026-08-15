// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Compliance.GDPR.for_PIIManager.when_deleting_an_encryption_key;

/// <summary>
/// Three calls happening is not the property that matters - the order they happen in is.
/// </summary>
/// <remarks>
/// Recording the erasure after the key material is gone leaves a window in which the key is absent and nothing
/// refuses a replacement, which is exactly the window the fence exists to close: a forwarded event landing in it
/// copies a surviving key straight back in. Evicting before the key is durably gone tells every peer silo to drop
/// it and then leaves them free to read it back out of a store that still holds it, and re-cache it. So the fence
/// goes first, the destruction second, and the eviction last.
/// </remarks>
public class and_the_durable_erase_succeeds : given.a_pii_manager
{
    Task Because() => _manager.DeleteEncryptionKeyFor(Identifier);

    [Fact] void should_record_the_erasure() => _keyStore.Received(1).RecordErasureFor(EventStore, EventStoreNamespace, Identifier);
    [Fact] void should_erase_the_key_from_storage() => _keyStore.Received(1).DeleteFor(EventStore, EventStoreNamespace, Identifier);
    [Fact] void should_evict_the_key_from_every_silo() => _cacheClient.Received(1).Evict(EventStore, EventStoreNamespace, Identifier);
    [Fact] void should_fence_the_key_before_erasing_and_evicting_it() => Received.InOrder(() =>
    {
        _keyStore.RecordErasureFor(EventStore, EventStoreNamespace, Identifier);
        _keyStore.DeleteFor(EventStore, EventStoreNamespace, Identifier);
        _cacheClient.Evict(EventStore, EventStoreNamespace, Identifier);
    });
}
