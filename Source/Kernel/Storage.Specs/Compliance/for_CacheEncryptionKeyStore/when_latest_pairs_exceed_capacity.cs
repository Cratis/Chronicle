// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Compliance.for_CacheEncryptionKeyStore;

public class when_latest_pairs_exceed_capacity : given.a_cache_with_present_keys
{
    int _count;
    EncryptionKeyAtRevision? _reloaded;

    async Task Because()
    {
        for (var index = 0; index <= CacheEncryptionKeyStorage.CacheCapacity; index++)
        {
            await _store.TryGetLatestFor("store", "ns", $"subject-{index}");
        }

        _count = _store.Counts.LatestPairs;
        _reloaded = await _store.TryGetLatestFor("store", "ns", "subject-0");
    }

    [Fact] void should_keep_only_the_capacity_in_latest_pairs() => _count.ShouldEqual(CacheEncryptionKeyStorage.CacheCapacity);
    [Fact] void should_reload_the_evicted_pair() => _reloaded.ShouldEqual(_pair);
    [Fact] void should_read_the_evicted_pair_from_storage_again() => _actualStore.Received(2).TryGetLatestFor("store", "ns", "subject-0");
}
