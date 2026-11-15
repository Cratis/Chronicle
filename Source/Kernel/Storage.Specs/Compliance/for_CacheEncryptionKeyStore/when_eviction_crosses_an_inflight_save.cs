// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;

namespace Cratis.Chronicle.Storage.Compliance.for_CacheEncryptionKeyStore;

public class when_eviction_crosses_an_inflight_save : given.a_cache_encryption_key_store
{
    readonly EncryptionKeyIdentifier _identifier = "subject";
    readonly EncryptionKey _key = new([1], [2]);
    readonly TaskCompletionSource _saved = new(TaskCreationOptions.RunContinuationsAsynchronously);
    EncryptionKey _result;

    void Establish() => _actualStore.SaveFor("store", "ns", _identifier, _key).Returns(_saved.Task);

    async Task Because()
    {
        var save = _store.SaveFor("store", "ns", _identifier, _key);
        _store.EvictFromCache("store", "ns", _identifier);
        _saved.SetResult();
        await save;
        _result = await _store.TryGetFor("store", "ns", _identifier);
    }

    [Fact] void should_not_repopulate_the_cache_after_eviction() => _result.ShouldBeNull();
}
