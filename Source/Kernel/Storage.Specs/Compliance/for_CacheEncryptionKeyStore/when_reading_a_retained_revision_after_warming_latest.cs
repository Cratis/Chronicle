// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Storage.Compliance.for_CacheEncryptionKeyStore;

public class when_reading_a_retained_revision_after_warming_latest : Specification
{
    readonly EncryptionKey _old = new([1], [2]);
    readonly EncryptionKey _new = new([3], [4]);
    CacheEncryptionKeyStorage _cache;
    EncryptionKeyAtRevision _latest;
    EncryptionKeyAtRevision _retained;

    async Task Establish()
    {
        var storage = new InMemoryEncryptionKeyStorage();
        await storage.SaveFor(EventStoreName.NotSet, EventStoreNamespaceName.Default, "subject", _old, 1);
        await storage.SaveFor(EventStoreName.NotSet, EventStoreNamespaceName.Default, "subject", _new, 2);
        _cache = new(storage);
        _latest = await _cache.TryGetLatestFor(EventStoreName.NotSet, EventStoreNamespaceName.Default, "subject");
    }

    async Task Because() => _retained = await _cache.TryGetAtRevisionFor(EventStoreName.NotSet, EventStoreNamespaceName.Default, "subject", 1);

    [Fact] void should_keep_the_latest_revision() => _latest.Revision.ShouldEqual((EncryptionKeyRevision)2u);
    [Fact] void should_return_the_retained_key() => _retained.Key.ShouldEqual(_old);
    [Fact] void should_keep_the_recorded_revision() => _retained.Revision.ShouldEqual((EncryptionKeyRevision)1u);
}
