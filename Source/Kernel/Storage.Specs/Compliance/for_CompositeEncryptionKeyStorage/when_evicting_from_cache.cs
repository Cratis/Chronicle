// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Storage.Compliance.for_CompositeEncryptionKeyStorage;

public class when_evicting_from_cache : given.two_key_stores
{
    EncryptionKey? _beforeEviction;
    EncryptionKey? _afterEviction;

    async Task Establish()
    {
        // The registered storage on a silo is the composite over the cached stores each backend registers, so the
        // cluster-wide crypto-shred fan-out reaches the composite and nothing else. Both inner caches have to be
        // cleared through it, or a peer silo keeps answering with a shredded key from a cache nothing ever clears.
        _composite = new CompositeEncryptionKeyStorage(new CacheEncryptionKeyStorage(_primary), new CacheEncryptionKeyStorage(_secondary));
        await Save(_secondary, KeyNamed("shredded"));
    }

    async Task Because()
    {
        // Warm both inner caches - the read heals the key into the primary through its cache as well.
        await _composite.TryGetFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier);

        // A peer silo performed the crypto-shred, so the backing stores no longer hold the key.
        await _primary.DeleteFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier);
        await _secondary.DeleteFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier);
        _beforeEviction = await _composite.TryGetFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier);

        _composite.EvictFromCache(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier);
        _afterEviction = await _composite.TryGetFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier);
    }

    [Fact] void should_serve_the_stale_key_until_the_eviction_arrives() => _beforeEviction.ShouldNotBeNull();
    [Fact] void should_carry_the_eviction_into_every_inner_cache() => _afterEviction.ShouldBeNull();
}
