// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Events.Constraints;
using Cratis.Chronicle.Storage.Sql.Sinks;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Constraints.for_UniqueConstraintEncodingStorage.when_reading_an_absent_key;

[Collection(PostgreSqlCollection.Name)]
public class on_postgresql(PostgreSqlFixture fixture) : given.a_postgresql_key_storage(fixture)
{
    UniqueConstraintEncodingKey? _result;

    async Task Because() => _result = await _storage.GetKey();

    [Fact] void should_return_null() => _result.ShouldBeNull();
}
