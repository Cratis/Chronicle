// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Compliance.for_CacheEncryptionKeyStore;

public class when_generation_pruning_crosses_a_save_with_cached_absence : given.a_cache_with_generation_pruning
{
    EncryptionKey? _result;
    int _absencesAfterSave;

    void Establish() => _actualStore.SaveFor("store", "ns", "subject", _newKey).Returns(_saved.Task);

    async Task Because()
    {
        var save = _store.SaveFor("store", "ns", "subject", _newKey);
        await _store.TryGetFor("store", "ns", "subject");
        PruneGenerations();
        _actualStore.TryGetFor("store", "ns", "subject").Returns(_newKey);
        _saved.SetResult();
        await save;
        _absencesAfterSave = _store.Counts.AbsentKeys;
        _result = await _store.TryGetFor("store", "ns", "subject");
    }

    [Fact] void should_invalidate_the_cached_absence() => _absencesAfterSave.ShouldEqual(0);
    [Fact] void should_return_the_newly_saved_key() => _result.ShouldEqual(_newKey);
    [Fact] void should_read_storage_again() => _actualStore.Received(2).TryGetFor("store", "ns", "subject");
}
