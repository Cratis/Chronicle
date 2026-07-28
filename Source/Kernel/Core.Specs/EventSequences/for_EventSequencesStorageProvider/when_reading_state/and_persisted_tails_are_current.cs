// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.EventSequences.for_EventSequencesStorageProvider.when_reading_state;

public class and_persisted_tails_are_current : given.the_provider
{
    GrainId _grainId;
    IGrainState<EventSequenceState> _state = null!;
    EventSequenceNumber _actualTailSequenceNumber;
    EventType _eventType;

    void Establish()
    {
        _actualTailSequenceNumber = 20;
        _eventType = new EventType("event-type", EventTypeGeneration.First);
        _grainId = GrainId.Create("type", new EventSequenceKey("sequence", "event-store", "default").ToString());
        _state = new GrainState<EventSequenceState> { State = new(), ETag = "etag", RecordExists = true };

        // The persisted snapshot was written at the current tail (a clean deactivation), so the per-event-type tails
        // are trustworthy and must not be re-derived from the events.
        eventSequenceStorage.GetState().Returns(Task.FromResult(new EventSequenceState
        {
            SequenceNumber = _actualTailSequenceNumber.Next(),
            TailSequenceNumberPerEventType = new Dictionary<EventTypeId, EventSequenceNumber> { [_eventType.Id] = _actualTailSequenceNumber }
        }));
        eventSequenceStorage.GetTailSequenceNumber().Returns(Task.FromResult(_actualTailSequenceNumber));
    }

    Task Because() => provider.ReadStateAsync("name", _grainId, _state);

    [Fact] void should_set_next_sequence_number_from_actual_tail() => _state.State.SequenceNumber.ShouldEqual(_actualTailSequenceNumber.Next());
    [Fact] void should_not_rebuild_tails_from_the_events() => eventSequenceStorage.DidNotReceive().GetTailSequenceNumbersForEventTypes(Arg.Any<IEnumerable<EventType>>());
    [Fact] void should_keep_the_persisted_tail() => _state.State.TailSequenceNumberPerEventType[_eventType.Id].ShouldEqual(_actualTailSequenceNumber);
}
