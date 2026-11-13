// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Compliance.for_CacheEncryptionKeyStore;

public class when_material_keys_exceed_capacity : given.a_cache_with_present_keys
{
    int _count;
    EncryptionKey? _reloaded;

    async Task Because()
    {
        for (var index = 0; index <= CacheEncryptionKeyStorage.CacheCapacity; index++)
        {
            await _store.TryGetFor("store", "ns", $"subject-{index}");
        }

        _count = _store.Counts.Keys;
        _reloaded = await _store.TryGetFor("store", "ns", "subject-0");
    }

    [Fact] void should_keep_only_the_capacity_in_material() => _count.ShouldEqual(CacheEncryptionKeyStorage.CacheCapacity);
    [Fact] void should_reload_the_evicted_key() => _reloaded.ShouldEqual(_key);
    [Fact] void should_read_the_evicted_key_from_storage_again() => _actualStore.Received(2).TryGetFor("store", "ns", "subject-0");
}
