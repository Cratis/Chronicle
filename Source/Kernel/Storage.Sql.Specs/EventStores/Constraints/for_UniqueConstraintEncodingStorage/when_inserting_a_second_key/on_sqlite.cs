// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Events.Constraints;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Constraints.for_UniqueConstraintEncodingStorage.when_inserting_a_second_key;

public class on_sqlite : given.a_sqlite_key_storage
{
    UniqueConstraintEncodingKey _original;
    UniqueConstraintEncodingKey _result;
    UniqueConstraintEncodingKey _stored;

    async Task Establish()
    {
        _original = UniqueConstraintEncodingKey.Create();
        await _storage.CreateKeyIfAbsent(_original);
    }

    async Task Because()
    {
        _result = await _storage.CreateKeyIfAbsent(UniqueConstraintEncodingKey.Create());
        _stored = (await _storage.GetKey())!;
    }

    [Fact] void should_return_the_original_identity() => _result.KeyId.ShouldEqual(_original.KeyId);
    [Fact] void should_return_the_original_material() => Assert.Equal(_original.Material.CopyBytes(), _result.Material.CopyBytes());
    [Fact] void should_keep_the_original_identity() => _stored.KeyId.ShouldEqual(_original.KeyId);
    [Fact] void should_keep_the_original_material() => Assert.Equal(_original.Material.CopyBytes(), _stored.Material.CopyBytes());
}
