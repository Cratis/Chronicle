// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Compliance.for_CacheEncryptionKeyStore;

public class when_capacity_eviction_overlaps_erasure_during_an_in_flight_read : given.an_in_flight_key_read
{
    async Task Because()
    {
        var pending = _store.TryGetFor("store", "ns", "subject");
        await EraseSubject();
        for (var index = 0; index <= CacheEncryptionKeyStorage.CacheCapacity; index++)
        {
            await _store.TryGetFor("store", "ns", $"other-{index}");
        }

        await CompleteRead(pending);
    }

    [Fact] void should_bound_material_entries() => _store.Counts.Keys.ShouldEqual(CacheEncryptionKeyStorage.CacheCapacity);
    [Fact] void should_not_return_the_pre_erasure_key() => _result.ShouldBeNull();
    [Fact] void should_not_republish_the_pre_erasure_key() => _later.ShouldBeNull();
    [Fact] void should_recheck_the_backing_store() => _actualStore.Received(3).TryGetFor("store", "ns", "subject");
}
