// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;

namespace Cratis.Chronicle.Storage.Compliance.for_CacheEncryptionKeyStore.when_getting_the_latest_key_with_its_revision;

public class and_the_cache_is_evicted : Specification
{
    IEncryptionKeyStorage _inner;
    CacheEncryptionKeyStorage _cache;
    EncryptionKeyAtRevision _pair;
    EncryptionKeyAtRevision? _result;

    void Establish()
    {
        _inner = Substitute.For<IEncryptionKeyStorage>();
        _pair = new(new EncryptionKey([1], [2]), new EncryptionKeyRevision(7u));
        _inner.TryGetLatestFor("store", "tenant", "subject").Returns(_pair);
        _cache = new(_inner);
    }

    async Task Because()
    {
        await _cache.TryGetLatestFor("store", "tenant", "subject");
        _cache.EvictFromCache("store", "tenant", "subject");
        _result = await _cache.TryGetLatestFor("store", "tenant", "subject");
    }

    [Fact] void should_return_the_pair_read_after_eviction() => _result.ShouldEqual(_pair);
    [Fact] async Task should_read_storage_again() => await _inner.Received(2).TryGetLatestFor("store", "tenant", "subject");
}
