// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Compliance.for_CacheEncryptionKeyStore.given;

public class a_cache_with_generation_pruning : a_cache_encryption_key_store
{
    protected readonly EncryptionKey _oldKey = new([1], [2]);
    protected readonly EncryptionKey _newKey = new([3], [4]);
    protected readonly TaskCompletionSource _saved = new(TaskCreationOptions.RunContinuationsAsynchronously);

    void Establish() => _store = new(_actualStore, new ControllableTimeProvider());

    protected void PruneGenerations()
    {
        for (var index = 0; index <= CacheEncryptionKeyStorage.CacheCapacity; index++)
        {
            _store.EvictFromCache("store", "ns", $"other-{index}");
        }
    }
}
