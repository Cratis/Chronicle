// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Storage.Compliance.for_CacheEncryptionKeyStore;

/// <summary>
/// The fence is written before the key material is destroyed, so between those two steps the backing store still
/// holds a key. A cache that kept serving it in that window would hand the key to whatever raced the erasure - and
/// cached keys have no time-to-live, so the entry would outlive the erasure entirely.
/// </summary>
public class when_recording_an_erasure : given.a_cache_encryption_key_store
{
    static readonly EncryptionKeyIdentifier _identifier = "some-subject";
    static readonly EncryptionKey _key = new([1, 2, 3], [4, 5, 6]);

    EncryptionKey? _afterwards;

    async Task Establish()
    {
        _actualStore.TryGetFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier, null).Returns(_key);
        await _store.TryGetFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier);
    }

    async Task Because()
    {
        await _store.RecordErasureFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier);

        // The backing store no longer has it - the erasure destroys the material right after fencing it.
        _actualStore.TryGetFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier, null).Returns((EncryptionKey?)null);
        _afterwards = await _store.TryGetFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier);
    }

    [Fact] void should_record_the_erasure_in_the_backing_store() => _actualStore.Received(1).RecordErasureFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier);
    [Fact] void should_stop_serving_the_key_from_the_cache() => _afterwards.ShouldBeNull();
}
