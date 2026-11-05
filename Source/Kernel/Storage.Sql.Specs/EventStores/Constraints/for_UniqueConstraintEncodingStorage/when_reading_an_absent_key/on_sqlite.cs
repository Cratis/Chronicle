// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Events.Constraints;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Constraints.for_UniqueConstraintEncodingStorage.when_reading_an_absent_key;

public class on_sqlite : given.a_sqlite_key_storage
{
    UniqueConstraintEncodingKey? _result;

    async Task Because() => _result = await _storage.GetKey();

    [Fact] void should_return_null() => _result.ShouldBeNull();
}
