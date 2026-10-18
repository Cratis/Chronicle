// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;

namespace Cratis.Chronicle.Storage.Compliance.for_CacheEncryptionKeyStore.when_getting_the_latest_key_with_its_revision;

public class and_an_inflight_read_overlaps_erasure : Specification
{
    IEncryptionKeyStorage _inner;
    CacheEncryptionKeyStorage _cache;
    TaskCompletionSource<EncryptionKeyAtRevision?> _read;
    EncryptionKeyAtRevision _stale;
    EncryptionKeyAtRevision? _result;

    void Establish()
    {
        _inner = Substitute.For<IEncryptionKeyStorage>();
        _read = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _stale = new(new EncryptionKey([1], [2]), new EncryptionKeyRevision(7u));
        _inner.TryGetLatestFor("store", "tenant", "subject").Returns(_read.Task);
        _cache = new(_inner);
    }

    async Task Because()
    {
        var pending = _cache.TryGetLatestFor("store", "tenant", "subject");
        await _cache.RecordErasureFor("store", "tenant", "subject");
        _read.SetResult(_stale);
        await pending;
        _inner.TryGetLatestFor("store", "tenant", "subject").Returns(Task.FromResult<EncryptionKeyAtRevision?>(null));
        _result = await _cache.TryGetLatestFor("store", "tenant", "subject");
    }

    [Fact] void should_not_resurrect_the_pair_in_the_cache() => _result.ShouldBeNull();
    [Fact] async Task should_read_storage_again() => await _inner.Received(2).TryGetLatestFor("store", "tenant", "subject");
}
