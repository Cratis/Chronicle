// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.EventSequences.for_EventSequencesStorageProvider.when_reading_state;

public class and_tail_sequence_number_is_unavailable : given.the_provider
{
    static GrainId _grainId;
    static IGrainState<EventSequenceState> _state = null!;

    void Establish()
    {
        _grainId = GrainId.Create("type", new EventSequenceKey("sequence", "event-store", "default").ToString());
        _state = new GrainState<EventSequenceState> { State = new(), ETag = "etag", RecordExists = true };

        eventSequenceStorage.GetState().Returns(Task.FromResult(new EventSequenceState
        {
            SequenceNumber = 17,
            TailSequenceNumberPerEventType = new Dictionary<EventTypeId, EventSequenceNumber>()
        }));
        eventSequenceStorage.GetTailSequenceNumber().Returns(Task.FromResult(EventSequenceNumber.Unavailable));
        eventTypesStorage.GetLatestForAllEventTypes().Returns(Task.FromResult<IEnumerable<EventTypeSchema>>([]));
        eventSequenceStorage.GetTailSequenceNumbersForEventTypes(Arg.Any<IEnumerable<EventType>>())
            .Returns(Task.FromResult<IImmutableDictionary<EventType, EventSequenceNumber>>(ImmutableDictionary<EventType, EventSequenceNumber>.Empty));
    }

    Task Because() => provider.ReadStateAsync("name", _grainId, _state);

    [Fact] void should_set_sequence_number_to_first() => _state.State.SequenceNumber.ShouldEqual(EventSequenceNumber.First);
}
