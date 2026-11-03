// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Storage.Compliance.for_CacheEncryptionKeyStore;

/// <summary>
/// Saving a later revision must not displace the earlier one. Rotation and idempotent-append comparison read earlier
/// revisions explicitly, so the cache has to keep serving the earlier key at its own revision without asking the
/// backing store, and must never delete anything while saving.
/// </summary>
public class when_saving_a_later_revision : given.a_cache_encryption_key_store
{
    static readonly EncryptionKeyIdentifier _identifier = "7c1e4b60-92d3-4a58-8f0e-5b6a3d2c9e41";
    static readonly EncryptionKeyRevision _earlierRevision = 1u;
    static readonly EncryptionKeyRevision _laterRevision = 2u;
    static readonly EncryptionKey _earlier = new([1], [2]);
    static readonly EncryptionKey _later = new([3], [4]);

    EncryptionKey? _atEarlier;
    EncryptionKey? _latest;

    async Task Establish() =>
        await _store.SaveFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier, _earlier, _earlierRevision);

    async Task Because()
    {
        await _store.SaveFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier, _later, _laterRevision);
        _atEarlier = await _store.TryGetFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier, _earlierRevision);
        _latest = await _store.TryGetFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier);
    }

    [Fact] void should_still_serve_the_earlier_key_at_its_revision() => _atEarlier.ShouldEqual(_earlier);
    [Fact] void should_not_go_to_the_backing_store_for_the_earlier_revision() => _actualStore.DidNotReceive().TryGetFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier, _earlierRevision);
    [Fact] void should_serve_the_later_key_as_latest() => _latest.ShouldEqual(_later);
    [Fact] void should_pass_the_later_revision_to_the_backing_store() => _actualStore.Received(1).SaveFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier, _later, _laterRevision);
    [Fact] void should_not_delete_anything_in_the_backing_store() => _actualStore.DidNotReceiveWithAnyArgs().DeleteFor(default!, default!, default!, default);
}
