// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.EventSequences.for_EventSequencesStorageProvider.when_reading_state;

public class and_persisted_tails_are_stale_after_a_crash : given.the_provider
{
    GrainId _grainId;
    IGrainState<EventSequenceState> _state = null!;
    EventSequenceNumber _actualTailSequenceNumber;
    EventType _eventTypeA;
    EventType _eventTypeB;
    EventSequenceNumber _rebuiltTailForA;
    EventSequenceNumber _rebuiltTailForB;

    void Establish()
    {
        _actualTailSequenceNumber = 20;
        _eventTypeA = new EventType("event-type-a", EventTypeGeneration.First);
        _eventTypeB = new EventType("event-type-b", EventTypeGeneration.First);
        _rebuiltTailForA = 15;
        _rebuiltTailForB = 20;

        _grainId = GrainId.Create("type", new EventSequenceKey("sequence", "event-store", "default").ToString());
        _state = new GrainState<EventSequenceState> { State = new(), ETag = "etag", RecordExists = true };

        // The persisted snapshot was written at sequence number 10, but appends continued to 20 before the crash,
        // so the persisted per-event-type tail for event-type-a (8) is stale.
        eventSequenceStorage.GetState().Returns(Task.FromResult(new EventSequenceState
        {
            SequenceNumber = 10,
            TailSequenceNumberPerEventType = new Dictionary<EventTypeId, EventSequenceNumber> { [_eventTypeA.Id] = 8 }
        }));
        eventSequenceStorage.GetTailSequenceNumber().Returns(Task.FromResult(_actualTailSequenceNumber));
        eventSequenceStorage.GetTailSequenceNumbersForEventTypes(Arg.Any<IEnumerable<EventType>>())
            .Returns(Task.FromResult<IImmutableDictionary<EventType, EventSequenceNumber>>(
                new Dictionary<EventType, EventSequenceNumber>
                {
                    [_eventTypeA] = _rebuiltTailForA,
                    [_eventTypeB] = _rebuiltTailForB
                }.ToImmutableDictionary()));
    }

    Task Because() => provider.ReadStateAsync("name", _grainId, _state);

    [Fact] void should_set_next_sequence_number_from_actual_tail() => _state.State.SequenceNumber.ShouldEqual(_actualTailSequenceNumber.Next());
    [Fact] void should_rebuild_tails_from_the_events() => eventSequenceStorage.Received(1).GetTailSequenceNumbersForEventTypes(Arg.Any<IEnumerable<EventType>>());
    [Fact] void should_replace_the_stale_tail_for_event_type_a() => _state.State.TailSequenceNumberPerEventType[_eventTypeA.Id].ShouldEqual(_rebuiltTailForA);
    [Fact] void should_include_the_rebuilt_tail_for_event_type_b() => _state.State.TailSequenceNumberPerEventType[_eventTypeB.Id].ShouldEqual(_rebuiltTailForB);
}
