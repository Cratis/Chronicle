// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Events.Constraints;
using Cratis.Chronicle.Storage.Sql.Sinks;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Constraints.for_UniqueConstraintEncodingStorage.when_inserting_a_second_key;

[Collection(SqlServerCollection.Name)]
public class on_sqlserver(SqlServerFixture fixture) : given.a_sqlserver_key_storage(fixture)
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
