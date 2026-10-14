// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.EventSequences.for_EventSequencesStorageProvider.when_reading_state;

public class and_a_watermark_is_persisted : given.the_provider
{
    GrainId _grainId;
    IGrainState<EventSequenceState> _state;
    ConstraintName[] _names = ["email"];

    void Establish()
    {
        _grainId = GrainId.Create("type", new EventSequenceKey("sequence", "event-store", "default").ToString());
        _state = new GrainState<EventSequenceState> { State = new() };
        eventSequenceStorage.GetState().Returns(new EventSequenceState
        {
            SequenceNumber = 10,
            ConstraintIndexWatermark = 5,
            ConstraintIndexCatchUpBelow = 4,
            ConstraintIndexCatchUpRefusing = _names
        });
        eventSequenceStorage.GetTailSequenceNumber().Returns((EventSequenceNumber)20);
    }

    Task Because() => provider.ReadStateAsync("name", _grainId, _state);

    [Fact] void should_rebuild_the_next_number_from_the_tail() => _state.State.SequenceNumber.Value.ShouldEqual(21UL);
    [Fact] void should_preserve_the_watermark() => _state.State.ConstraintIndexWatermark!.Value.ShouldEqual(5UL);
    [Fact] void should_preserve_the_boundary() => _state.State.ConstraintIndexCatchUpBelow!.Value.ShouldEqual(4UL);
    [Fact] void should_preserve_the_refusing_constraints() => _state.State.ConstraintIndexCatchUpRefusing.ShouldEqual(_names);
}
