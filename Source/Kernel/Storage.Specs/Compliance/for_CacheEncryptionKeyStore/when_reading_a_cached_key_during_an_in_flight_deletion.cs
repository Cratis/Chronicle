// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Storage.Compliance.for_CacheEncryptionKeyStore;

/// <summary>
/// <see cref="when_reading_during_an_in_flight_deletion"/> covers the read that misses a cold cache while the erase is
/// in flight; this covers the read that would have been answered out of a warm one. Erasure invalidates on both sides
/// of the durable call, and it is the invalidation that runs first - before the backing store is even asked - that
/// stops this silo serving the key it already holds for the whole duration of the erase. That duration is not short:
/// over a composite store or a remote vault the erase takes as long as the slowest store it has to reach, and every
/// read arriving in the meantime would otherwise be handed the key from memory without the backing store being
/// consulted at all. The invalidation that follows the erase leaves the same end state either way, so the only way to
/// see this one is to warm the cache first and watch where the racing read goes.
/// </summary>
public class when_reading_a_cached_key_during_an_in_flight_deletion : given.a_cache_encryption_key_store
{
    static readonly EncryptionKeyIdentifier _identifier = "b41e9d07-2c85-4a63-9f1e-8d0b7c6a5e34";
    static readonly EncryptionKey _key = new([1], [2]);

    readonly TaskCompletionSource _erasureStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _releaseErasure = new(TaskCreationOptions.RunContinuationsAsynchronously);
    EncryptionKey? _duringErasure;

    async Task Establish()
    {
        _actualStore
            .DeleteFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier)
            .Returns(_ => EraseBlocked());

        _actualStore
            .TryGetFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier)
            .Returns(Task.FromResult<EncryptionKey?>(_key));

        await _store.SaveFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier, _key);
    }

    async Task Because()
    {
        var inFlightErasure = _store.DeleteFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier);
        await _erasureStarted.Task;

        _duringErasure = await _store.TryGetFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier);

        _releaseErasure.SetResult();
        await inFlightErasure;
    }

    async Task EraseBlocked()
    {
        _erasureStarted.SetResult();
        await _releaseErasure.Task;
    }

    [Fact] void should_stop_answering_from_the_cache_the_moment_the_erasure_was_requested() => _actualStore.Received(1).TryGetFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier);
    [Fact] void should_hand_the_racing_read_what_the_backing_store_still_held() => _duringErasure.ShouldEqual(_key);
}
