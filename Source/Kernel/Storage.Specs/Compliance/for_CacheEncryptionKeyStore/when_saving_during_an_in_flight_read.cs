// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Storage.Compliance.for_CacheEncryptionKeyStore;

/// <summary>
/// A save is the third thing that mutates the cache, and it is the one a read cannot see coming. The read captures the
/// generation, waits on the backing store, and comes back holding the key that was current when it asked. If the save
/// did not move the generation, the read writes that superseded key back over the one the save just cached - and
/// because cached keys have no time-to-live, every later read is served the wrong revision until something evicts it.
/// For a rotated key that means values encrypted under the new revision stop decrypting.
/// </summary>
public class when_saving_during_an_in_flight_read : given.a_cache_encryption_key_store
{
    static readonly EncryptionKeyIdentifier _identifier = "2d9f8c47-6b13-4a55-8e72-0f1a3b5c7d92";
    static readonly EncryptionKey _supersededKey = new([1], [2]);
    static readonly EncryptionKey _savedKey = new([3], [4]);

    readonly TaskCompletionSource _readStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _releaseRead = new(TaskCreationOptions.RunContinuationsAsynchronously);
    EncryptionKey? _duringSave;
    EncryptionKey? _afterSave;

    void Establish() =>
        _actualStore
            .TryGetFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier)
            .Returns(_ => ReadBlockedSupersededKey());

    async Task Because()
    {
        var inFlightRead = _store.TryGetFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier);
        await _readStarted.Task;
        await _store.SaveFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier, _savedKey);
        _releaseRead.SetResult();
        _duringSave = await inFlightRead;

        _afterSave = await _store.TryGetFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier);
    }

    async Task<EncryptionKey?> ReadBlockedSupersededKey()
    {
        _readStarted.SetResult();
        await _releaseRead.Task;
        return _supersededKey;
    }

    [Fact] void should_hand_the_racing_read_what_the_backing_store_held_when_it_asked() => _duringSave.ShouldEqual(_supersededKey);
    [Fact] void should_serve_the_saved_key_afterwards() => _afterSave.ShouldEqual(_savedKey);
    [Fact] void should_serve_it_from_the_cache() => _actualStore.Received(1).TryGetFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier);
}
