// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;

namespace Cratis.Chronicle.Storage.Compliance.for_CompositeEncryptionKeyStorage.when_getting_the_latest_key_with_its_revision;

public class and_the_stores_disagree : Specification
{
    InMemoryEncryptionKeyStorage _first;
    InMemoryEncryptionKeyStorage _second;
    CompositeEncryptionKeyStorage _composite;
    EncryptionKey _key = new([1], [2]);
    EncryptionKeyAtRevision? _result;

    async Task Establish()
    {
        _first = new();
        _second = new();
        _composite = new(_first, _second);
        await _first.SaveFor("store", "tenant", "subject", _key, new EncryptionKeyRevision(7u));
        await _second.SaveFor("store", "tenant", "subject", new EncryptionKey([3], [4]), new EncryptionKeyRevision(8u));
    }

    async Task Because() => _result = await _composite.TryGetLatestFor("store", "tenant", "subject");

    [Fact] void should_return_the_first_stores_key() => _result!.Key.ShouldEqual(_key);
    [Fact] void should_return_the_first_stores_revision() => _result!.Revision.ShouldEqual(new EncryptionKeyRevision(7u));
    [Fact] async Task should_match_the_key_only_read() => (await _composite.TryGetFor("store", "tenant", "subject")).ShouldEqual(_result!.Key);
}
