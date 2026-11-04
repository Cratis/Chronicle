// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.EventSequences.for_EventSequencesStorageProvider.when_reading_state;

public class and_the_sequence_name_is_reserved_but_already_exists : given.the_provider
{
    readonly EventSequenceKey _key = new("observers", "event-store", "default");
    readonly GrainState<EventSequenceState> _state = new() { State = new() };

    void Establish()
    {
        reservedEventSequenceIds.IsReserved(_key.EventSequenceId).Returns(true);
        storage.GetEventStores().Returns([_key.EventStore]);
        eventStoreNamespaceStorage.GetEventSequences().Returns(["observers"]);
        eventSequenceStorage.GetState().Returns(new EventSequenceState());
        eventSequenceStorage.GetTailSequenceNumber().Returns(EventSequenceNumber.Unavailable);
    }

    Task Because() => provider.ReadStateAsync("name", GrainId.Create("type", _key.ToString()), _state);

    [Fact] void should_read_sequence_state() => eventSequenceStorage.Received(1).GetState();
    [Fact] void should_read_the_tail() => eventSequenceStorage.Received(1).GetTailSequenceNumber();
    [Fact] void should_leave_the_next_position_at_first() => _state.State.SequenceNumber.ShouldEqual(EventSequenceNumber.First);
}
