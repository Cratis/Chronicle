// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.Storage.InMemory.EventSequences.for_EventSequenceStorage.when_saving_state;

public class and_a_mutation_epoch_is_set : given.an_event_sequence_storage
{
    EventSequenceState _result;

    async Task Because()
    {
        await _storage.SaveState(new EventSequenceState { MutationEpoch = 42 });
        _result = await _storage.GetState();
    }

    [Fact] void should_round_trip_the_epoch() => _result.MutationEpoch.Value.ShouldEqual(42UL);
}
