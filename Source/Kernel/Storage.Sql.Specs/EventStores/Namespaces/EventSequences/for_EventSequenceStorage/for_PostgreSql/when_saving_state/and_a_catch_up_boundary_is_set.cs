// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.Sinks;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceStorage.for_PostgreSql.when_saving_state;

[Collection(PostgreSqlCollection.Name)]
public class and_a_catch_up_boundary_is_set(PostgreSqlFixture fixture) : given.a_postgresql_state_storage(fixture)
{
    Chronicle.Storage.EventSequences.EventSequenceState _result;

    async Task Because()
    {
        await _storage.SaveState(new() { ConstraintIndexCatchUpBelow = ulong.MaxValue - 2 });
        _result = await _storage.GetState();
    }

    [Fact] void should_round_trip_the_unsigned_boundary_exactly() => _result.ConstraintIndexCatchUpBelow!.Value.ShouldEqual(ulong.MaxValue - 2);
}
