// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;

namespace Cratis.Chronicle.Storage.Compliance.for_CompositeEncryptionKeyStorage.when_getting_the_latest_key_with_its_revision;

public class and_the_first_store_holds_destroyed_material : Specification
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
        await _second.SaveFor("store", "tenant", "subject", _key, new EncryptionKeyRevision(7u));
        await _second.RecordErasureFor("store", "tenant", "subject");
        await _second.DeleteFor("store", "tenant", "subject");
    }

    async Task Because() => _result = await _composite.TryGetLatestFor("store", "tenant", "subject");

    [Fact] void should_refuse_the_destroyed_material() => _result.ShouldBeNull();
    [Fact] async Task should_not_heal_the_destroyed_material() => (await _second.HasFor("store", "tenant", "subject")).ShouldBeFalse();
}
