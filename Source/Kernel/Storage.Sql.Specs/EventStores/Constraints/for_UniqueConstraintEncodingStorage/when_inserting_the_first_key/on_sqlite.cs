// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Storage.Events.Constraints;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Constraints.for_UniqueConstraintEncodingStorage.when_inserting_the_first_key;

public class on_sqlite : given.a_sqlite_key_storage
{
    UniqueConstraintEncodingKey _candidate;
    UniqueConstraintEncodingKey _result;
    UniqueConstraintEncodingKey _stored;

    void Establish()
    {
        // Contains 0x00 and 0xFF at the edges to prove exact binary round trips.
        var material = new byte[32];
        material[0] = 0x00;
        material[1] = 0xFF;
        material[31] = 0xFF;
        _candidate = new UniqueConstraintEncodingKey(new UniqueConstraintStoreKeyId("00ff10a5b6c7d8e9f0123456789abcde"), new UniqueConstraintStoreKey(material));
    }

    async Task Because()
    {
        _result = await _storage.CreateKeyIfAbsent(_candidate);
        _stored = (await _storage.GetKey())!;
    }

    [Fact] void should_return_the_candidate() => _result.ShouldEqual(_candidate);
    [Fact] void should_read_the_same_key_id() => _stored.KeyId.ShouldEqual(_candidate.KeyId);
    [Fact] void should_read_identical_material() => Assert.Equal(_candidate.Material.CopyBytes(), _stored.Material.CopyBytes());
}
