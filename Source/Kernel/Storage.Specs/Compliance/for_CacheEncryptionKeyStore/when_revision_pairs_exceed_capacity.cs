// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;

namespace Cratis.Chronicle.Storage.Compliance.for_CacheEncryptionKeyStore;

public class when_revision_pairs_exceed_capacity : given.a_cache_with_present_keys
{
    int _count;
    EncryptionKeyAtRevision? _reloaded;

    async Task Because()
    {
        for (uint revision = 1; revision <= CacheEncryptionKeyStorage.CacheCapacity + 1; revision++)
        {
            await _store.TryGetAtRevisionFor("store", "ns", "subject", revision);
        }

        _count = _store.Counts.RevisionPairs;
        _reloaded = await _store.TryGetAtRevisionFor("store", "ns", "subject", EncryptionKeyRevision.Initial);
    }

    [Fact] void should_bound_even_many_revisions_of_one_identifier() => _count.ShouldEqual(CacheEncryptionKeyStorage.CacheCapacity);
    [Fact] void should_reload_the_evicted_pair() => _reloaded.ShouldEqual(_pair);
    [Fact] void should_read_the_evicted_revision_from_storage_again() => _actualStore.Received(2).TryGetAtRevisionFor("store", "ns", "subject", EncryptionKeyRevision.Initial);
}
