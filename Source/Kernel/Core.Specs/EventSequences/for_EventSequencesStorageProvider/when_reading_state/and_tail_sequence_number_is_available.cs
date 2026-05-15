// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.EventSequences.for_EventSequencesStorageProvider.when_reading_state;

public class and_tail_sequence_number_is_available : given.the_provider
{
    static GrainId _grainId;
    static IGrainState<EventSequenceState> _state = null!;
    static EventSequenceNumber _actualTailSequenceNumber;

    void Establish()
    {
        _actualTailSequenceNumber = 41;
        _grainId = GrainId.Create("type", new EventSequenceKey("sequence", "event-store", "default").ToString());
        _state = new GrainState<EventSequenceState> { State = new(), ETag = "etag", RecordExists = true };

        eventSequenceStorage.GetState().Returns(Task.FromResult(new EventSequenceState
        {
            SequenceNumber = 3,
            TailSequenceNumberPerEventType = new Dictionary<EventTypeId, EventSequenceNumber> { ["event-type"] = 2 }
        }));
        eventSequenceStorage.GetTailSequenceNumber().Returns(Task.FromResult(_actualTailSequenceNumber));
    }

    Task Because() => provider.ReadStateAsync("name", _grainId, _state);

    [Fact] void should_get_tail_sequence_number() => eventSequenceStorage.Received(1).GetTailSequenceNumber();
    [Fact] void should_set_next_sequence_number_from_actual_tail() => _state.State.SequenceNumber.ShouldEqual(_actualTailSequenceNumber.Next());
}
