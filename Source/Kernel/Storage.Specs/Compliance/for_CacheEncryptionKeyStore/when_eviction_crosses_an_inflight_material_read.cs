// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;

namespace Cratis.Chronicle.Storage.Compliance.for_CacheEncryptionKeyStore;

public class when_eviction_crosses_an_inflight_material_read : given.a_cache_encryption_key_store
{
    readonly EncryptionKeyIdentifier _identifier = "subject";
    readonly TaskCompletionSource<EncryptionKey?> _fetched = new(TaskCreationOptions.RunContinuationsAsynchronously);
    EncryptionKey _result;

    void Establish() => _actualStore.TryGetFor("store", "ns", _identifier).Returns(_fetched.Task, Task.FromResult<EncryptionKey?>(null));

    async Task Because()
    {
        var read = _store.TryGetFor("store", "ns", _identifier);
        _store.EvictFromCache("store", "ns", _identifier);
        _fetched.SetResult(new([1], [2]));
        _result = await read;
    }

    [Fact] void should_not_return_the_invalidated_key() => _result.ShouldBeNull();
    [Fact] void should_read_again() => _actualStore.Received(2).TryGetFor("store", "ns", _identifier);
}
