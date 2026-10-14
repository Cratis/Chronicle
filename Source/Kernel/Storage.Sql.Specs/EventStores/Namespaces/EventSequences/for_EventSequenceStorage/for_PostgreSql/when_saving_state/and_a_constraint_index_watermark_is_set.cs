// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.Sinks;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceStorage.for_PostgreSql.when_saving_state;

[Collection(PostgreSqlCollection.Name)]
public class and_a_constraint_index_watermark_is_set(PostgreSqlFixture fixture) : given.a_postgresql_state_storage(fixture)
{
    Chronicle.Storage.EventSequences.EventSequenceState _result;

    async Task Because()
    {
        await _storage.SaveState(new() { ConstraintIndexWatermark = ulong.MaxValue - 1 });
        _result = await _storage.GetState();
    }

    [Fact] void should_round_trip_the_unsigned_watermark_exactly() => _result.ConstraintIndexWatermark!.Value.ShouldEqual(ulong.MaxValue - 1);
}
