// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.Storage.InMemory.EventSequences.for_EventSequenceStorage.when_getting_state;

public class and_the_state_predates_the_mutation_epoch : given.an_event_sequence_storage
{
    EventSequenceState _result;

    async Task Because() => _result = await _storage.GetState();

    [Fact] void should_start_at_the_initial_epoch() => _result.MutationEpoch.ShouldEqual(Concepts.EventSequences.EventSequenceMutationEpoch.Initial);
}
