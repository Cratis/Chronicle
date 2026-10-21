// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.Storage.InMemory.EventSequences.for_EventSequenceStorage.when_saving_state;

public class and_a_constraint_index_watermark_is_set : given.an_event_sequence_storage
{
    EventSequenceState _result;

    async Task Because()
    {
        await _storage.SaveState(new EventSequenceState { ConstraintIndexWatermark = 42 });
        _result = await _storage.GetState();
    }

    [Fact] void should_round_trip_the_watermark() => _result.ConstraintIndexWatermark!.Value.ShouldEqual(42UL);
}
