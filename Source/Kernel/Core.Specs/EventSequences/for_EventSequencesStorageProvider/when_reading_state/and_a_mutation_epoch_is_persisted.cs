// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.EventSequences.for_EventSequencesStorageProvider.when_reading_state;

public class and_a_mutation_epoch_is_persisted : given.the_provider
{
    GrainId _grainId;
    IGrainState<EventSequenceState> _state;

    void Establish()
    {
        _grainId = GrainId.Create("type", new EventSequenceKey("sequence", "event-store", "default").ToString());
        _state = new GrainState<EventSequenceState> { State = new() };
        eventSequenceStorage.GetState().Returns(new EventSequenceState { SequenceNumber = 10, MutationEpoch = new EventSequenceMutationEpoch(6) });
        eventSequenceStorage.GetTailSequenceNumber().Returns((EventSequenceNumber)20);
    }

    Task Because() => provider.ReadStateAsync("name", _grainId, _state);

    [Fact] void should_preserve_the_mutation_epoch() => _state.State.MutationEpoch.Value.ShouldEqual(6UL);
}
