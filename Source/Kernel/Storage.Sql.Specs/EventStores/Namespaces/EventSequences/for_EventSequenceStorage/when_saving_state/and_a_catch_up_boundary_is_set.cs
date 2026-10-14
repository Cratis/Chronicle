// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceStorage.when_saving_state;

public class and_a_catch_up_boundary_is_set : given.a_state_storage
{
    Chronicle.Storage.EventSequences.EventSequenceState _result;

    async Task Because()
    {
        await _storage.SaveState(new() { ConstraintIndexCatchUpBelow = ulong.MaxValue - 2 });
        _result = await _storage.GetState();
    }

    [Fact] void should_round_trip_the_unsigned_boundary_exactly() => _result.ConstraintIndexCatchUpBelow!.Value.ShouldEqual(ulong.MaxValue - 2);
}
