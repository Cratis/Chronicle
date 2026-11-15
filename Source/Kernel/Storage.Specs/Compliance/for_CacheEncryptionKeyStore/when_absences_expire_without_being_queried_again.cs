// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Compliance.for_CacheEncryptionKeyStore;

public class when_absences_expire_without_being_queried_again : given.a_cache_encryption_key_store
{
    readonly ControllableTimeProvider _time = new();
    int _before;
    int _after;

    void Establish()
    {
        _store = new(_actualStore, _time);
        _actualStore.TryGetFor("store", "ns", "present").Returns(new EncryptionKey([1], [2]));
    }

    async Task Because()
    {
        for (var index = 0; index <= CacheEncryptionKeyStorage.CacheCapacity; index++)
        {
            await _store.TryGetFor("store", "ns", $"missing-{index}");
        }

        _before = _store.Counts.AbsentKeys;
        await _store.TryGetFor("store", "ns", "present");
        _time.Advance(CacheEncryptionKeyStorage.DefaultNegativeCacheTimeToLive + TimeSpan.FromSeconds(1));
        for (var index = 0; index < CacheEncryptionKeyStorage.SweepInterval; index++)
        {
            await _store.TryGetFor("store", "ns", "present");
        }

        _after = _store.Counts.AbsentKeys;
    }

    [Fact] void should_bound_absences_even_before_expiration() => _before.ShouldEqual(CacheEncryptionKeyStorage.CacheCapacity);
    [Fact] void should_sweep_all_expired_absences_during_unrelated_cache_hits() => _after.ShouldEqual(0);
    [Fact] void should_not_need_to_requery_the_missing_identifiers() => _actualStore.Received(1).TryGetFor("store", "ns", "missing-0");
    [Fact] void should_serve_the_unrelated_key_from_cache() => _actualStore.Received(1).TryGetFor("store", "ns", "present");
}
