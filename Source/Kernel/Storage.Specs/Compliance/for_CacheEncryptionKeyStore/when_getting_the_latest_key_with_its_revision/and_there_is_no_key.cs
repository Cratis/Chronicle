// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;

namespace Cratis.Chronicle.Storage.Compliance.for_CacheEncryptionKeyStore.when_getting_the_latest_key_with_its_revision;

public class and_there_is_no_key : Specification
{
    IEncryptionKeyStorage _inner;
    CacheEncryptionKeyStorage _cache;
    EncryptionKeyAtRevision _pair;
    EncryptionKeyAtRevision? _result;

    void Establish()
    {
        _inner = Substitute.For<IEncryptionKeyStorage>();
        _pair = new(new EncryptionKey([1], [2]), new EncryptionKeyRevision(7u));
        _inner.TryGetLatestFor("store", "tenant", "subject").Returns(_pair);
        _cache = new(_inner);
        _inner.TryGetLatestFor("store", "tenant", "subject").Returns(Task.FromResult<EncryptionKeyAtRevision?>(null));
    }

    async Task Because()
    {
        await _cache.TryGetLatestFor("store", "tenant", "subject");
        _result = await _cache.TryGetLatestFor("store", "tenant", "subject");
    }

    [Fact] void should_return_absence() => _result.ShouldBeNull();
    [Fact] async Task should_remember_absence() => await _inner.Received(1).TryGetLatestFor("store", "tenant", "subject");
    [Fact] void should_not_provision_a_key() => _inner.DidNotReceiveWithAnyArgs().GetOrAddWithRevisionFor(default!, default!, default!, default!);
}
