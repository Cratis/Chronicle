// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;

namespace Cratis.Chronicle.Storage.Compliance.for_CacheEncryptionKeyStore;

public class when_generation_pruning_crosses_a_later_revision_save : given.a_cache_with_generation_pruning
{
    readonly EncryptionKeyRevision _revision = 2;
    EncryptionKeyAtRevision _newPair;
    EncryptionKeyAtRevision? _result;
    int _pairsAfterSave;

    void Establish()
    {
        _newPair = new(_newKey, _revision);
        _actualStore.SaveFor("store", "ns", "subject", _newKey, _revision).Returns(_saved.Task);
        _actualStore.TryGetLatestFor("store", "ns", "subject").Returns(new EncryptionKeyAtRevision(_oldKey, EncryptionKeyRevision.Initial));
    }

    async Task Because()
    {
        var save = _store.SaveFor("store", "ns", "subject", _newKey, _revision);
        await _store.TryGetLatestFor("store", "ns", "subject");
        PruneGenerations();
        _actualStore.TryGetLatestFor("store", "ns", "subject").Returns(_newPair);
        _saved.SetResult();
        await save;
        _pairsAfterSave = _store.Counts.LatestPairs;
        _result = await _store.TryGetLatestFor("store", "ns", "subject");
    }

    [Fact] void should_invalidate_the_old_latest_pair() => _pairsAfterSave.ShouldEqual(0);
    [Fact] void should_return_the_new_latest_pair() => _result.ShouldEqual(_newPair);
    [Fact] void should_read_storage_again() => _actualStore.Received(2).TryGetLatestFor("store", "ns", "subject");
}
