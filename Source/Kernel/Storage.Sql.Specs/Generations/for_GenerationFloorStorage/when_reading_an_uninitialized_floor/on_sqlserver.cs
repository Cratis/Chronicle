// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Generations.Enablement;

using Cratis.Chronicle.Storage.Sql.Sinks;

namespace Cratis.Chronicle.Storage.Sql.Generations.for_GenerationFloorStorage.when_reading_an_uninitialized_floor;

[Collection(SqlServerCollection.Name)]
public class on_sqlserver(SqlServerFixture fixture) : given.a_sqlserver_floor_storage(fixture)
{
    GenerationFloor? _result;
    IReadOnlyList<GenerationFloor> _history;
    async Task Because()
    {
        _result = await _storage.Get();
        _history = await _storage.GetHistory();
    }
    [Fact] void should_return_no_floor() => _result.ShouldBeNull();
    [Fact] void should_leave_no_audit_rows() => _history.ShouldBeEmpty();
}
