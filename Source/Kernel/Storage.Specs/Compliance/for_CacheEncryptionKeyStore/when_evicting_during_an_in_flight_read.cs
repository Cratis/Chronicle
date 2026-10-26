// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Storage.Compliance.for_CacheEncryptionKeyStore;

public class when_evicting_during_an_in_flight_read : given.a_cache_encryption_key_store
{
    static readonly EncryptionKeyIdentifier _identifier = "5c6cce36-d60d-46db-9db2-e820559962db";
    static readonly EncryptionKey _key = new([1], [2]);

    readonly TaskCompletionSource _readStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _releaseRead = new(TaskCreationOptions.RunContinuationsAsynchronously);
    EncryptionKey? _afterEviction;

    void Establish() =>
        _actualStore
            .TryGetFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier)
            .Returns(_ => ReadBlockedKey());

    async Task Because()
    {
        var inFlightRead = _store.TryGetFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier);
        await _readStarted.Task;
        _store.EvictFromCache(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier);
        _releaseRead.SetResult();
        await inFlightRead;

        _actualStore
            .TryGetFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier)
            .Returns(Task.FromResult<EncryptionKey?>(null));
        _afterEviction = await _store.TryGetFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier);
    }

    async Task<EncryptionKey?> ReadBlockedKey()
    {
        _readStarted.SetResult();
        await _releaseRead.Task;
        return _key;
    }

    [Fact] void should_not_restore_the_evicted_key_to_the_cache() => _afterEviction.ShouldBeNull();
    [Fact] void should_read_the_backing_store_again() => _actualStore.Received(2).TryGetFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier);
}
