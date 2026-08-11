// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Storage.Compliance.for_CacheEncryptionKeyStore;

/// <summary>
/// The mirror image of <see cref="when_deleting_during_an_in_flight_read"/>: there the read reached the backing store
/// first and the deletion landed while it waited; here the deletion goes first and the read slips into the window
/// between the cache being invalidated and the erase becoming durable. Invalidating only before the erase is not
/// enough for this ordering - the read misses the cache, reads a key the store has not erased yet, and finds the
/// generation it captured unchanged when it comes back to write it. Cached keys have no time-to-live, so that entry
/// would outlive the erasure for as long as the process runs.
/// </summary>
public class when_reading_during_an_in_flight_deletion : given.a_cache_encryption_key_store
{
    static readonly EncryptionKeyIdentifier _identifier = "7a3d5e11-4b28-4f6c-9d0a-1c2e3f4a5b60";
    static readonly EncryptionKey _key = new([1], [2]);

    readonly TaskCompletionSource _erasureStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _releaseErasure = new(TaskCreationOptions.RunContinuationsAsynchronously);
    EncryptionKey? _duringErasure;
    EncryptionKey? _afterErasure;

    void Establish()
    {
        _actualStore
            .DeleteFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier)
            .Returns(_ => EraseBlocked());

        _actualStore
            .TryGetFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier)
            .Returns(Task.FromResult<EncryptionKey?>(_key));
    }

    async Task Because()
    {
        var inFlightErasure = _store.DeleteFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier);
        await _erasureStarted.Task;

        _duringErasure = await _store.TryGetFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier);

        _releaseErasure.SetResult();
        await inFlightErasure;

        _actualStore
            .TryGetFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier)
            .Returns(Task.FromResult<EncryptionKey?>(null));
        _afterErasure = await _store.TryGetFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier);
    }

    async Task EraseBlocked()
    {
        _erasureStarted.SetResult();
        await _releaseErasure.Task;
    }

    [Fact] void should_hand_the_racing_read_what_the_backing_store_still_held() => _duringErasure.ShouldEqual(_key);
    [Fact] void should_not_leave_the_erased_key_in_the_cache() => _afterErasure.ShouldBeNull();
    [Fact] void should_read_the_backing_store_again() => _actualStore.Received(2).TryGetFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier);
}
