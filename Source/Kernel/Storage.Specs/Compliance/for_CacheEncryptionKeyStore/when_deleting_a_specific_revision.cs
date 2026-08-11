// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Storage.Compliance.for_CacheEncryptionKeyStore;

/// <summary>
/// Erasing one revision of a key is not erasing the key. Rotation leaves earlier revisions in place so values written
/// under them still decrypt, and a backing store asked for a single revision erases only that one - so the cache has to
/// drop exactly what the erase can have removed and keep the rest. Dropping every revision of the identifier is not
/// wrong, only wasteful, and that is why it goes unnoticed: every later read for a revision that is still perfectly
/// live silently falls through to the backing store and gets the same answer back.
/// </summary>
public class when_deleting_a_specific_revision : given.a_cache_encryption_key_store
{
    static readonly EncryptionKeyIdentifier _identifier = "3f5c8a92-1e47-4d6b-b08f-7a2c9d4e6b13";
    static readonly EncryptionKeyRevision _erasedRevision = 1u;
    static readonly EncryptionKeyRevision _retainedRevision = 2u;
    static readonly EncryptionKey _erasedKey = new([1], [2]);
    static readonly EncryptionKey _retainedKey = new([3], [4]);

    EncryptionKey? _retainedAfterErasure;
    EncryptionKey? _erasedAfterErasure;

    async Task Establish()
    {
        _actualStore
            .TryGetFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier, _erasedRevision)
            .Returns(Task.FromResult<EncryptionKey?>(null));

        _actualStore
            .TryGetFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier, _retainedRevision)
            .Returns(Task.FromResult<EncryptionKey?>(null));

        await _store.SaveFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier, _erasedKey, _erasedRevision);
        await _store.SaveFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier, _retainedKey, _retainedRevision);
    }

    async Task Because()
    {
        await _store.DeleteFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier, _erasedRevision);

        _retainedAfterErasure = await _store.TryGetFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier, _retainedRevision);
        _erasedAfterErasure = await _store.TryGetFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier, _erasedRevision);
    }

    [Fact] void should_keep_serving_the_revision_that_was_not_erased() => _retainedAfterErasure.ShouldEqual(_retainedKey);
    [Fact] void should_not_go_to_the_backing_store_for_the_revision_it_kept() => _actualStore.DidNotReceive().TryGetFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier, _retainedRevision);
    [Fact] void should_no_longer_serve_the_revision_that_was_erased() => _erasedAfterErasure.ShouldBeNull();
    [Fact] void should_go_to_the_backing_store_for_the_revision_it_dropped() => _actualStore.Received(1).TryGetFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier, _erasedRevision);
}
