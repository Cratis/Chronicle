// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;

namespace Cratis.Chronicle.Storage.Compliance.for_CacheEncryptionKeyStore.when_getting_or_adding_with_revision;

public class and_the_provisioned_revision_was_remembered_absent : Specification
{
    IEncryptionKeyStorage _inner;
    CacheEncryptionKeyStorage _cache;
    EncryptionKeyAtRevision _pair;
    EncryptionKey? _result;

    async Task Establish()
    {
        _inner = Substitute.For<IEncryptionKeyStorage>();
        _pair = new(new EncryptionKey([1], [2]), new EncryptionKeyRevision(7u));
        _inner.GetOrAddWithRevisionFor("store", "tenant", "subject", _pair.Key).Returns(_pair);
        _cache = new(_inner);
        await _cache.TryGetFor("store", "tenant", "subject", _pair.Revision);
    }

    async Task Because()
    {
        await _cache.GetOrAddWithRevisionFor("store", "tenant", "subject", _pair.Key);
        _result = await _cache.TryGetFor("store", "tenant", "subject", _pair.Revision);
    }

    [Fact] void should_return_the_provisioned_key_for_decryption() => _result.ShouldEqual(_pair.Key);
    [Fact] async Task should_not_read_storage_again() => await _inner.Received(1).TryGetFor("store", "tenant", "subject", _pair.Revision);
}
