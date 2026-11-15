// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.EventSequences.for_EventSequencesStorageProvider.when_reading_state;

public class and_the_sequence_name_is_reserved_for_a_new_sequence : given.the_provider
{
    readonly EventSequenceKey _key = new("observers", "event-store", "default");
    readonly GrainState<EventSequenceState> _state = new() { State = new() };

    void Establish()
    {
        reservedEventSequenceIds.IsReserved(_key.EventSequenceId).Returns(true);
        storage.GetEventStores().Returns([_key.EventStore]);
        eventStoreNamespaceStorage.GetEventSequences().Returns([]);
        eventSequenceStorage.GetState().Returns(new EventSequenceState());
    }

    Task Because() => provider.ReadStateAsync("name", GrainId.Create("type", _key.ToString()), _state);

    [Fact] void should_leave_an_empty_state() => _state.State.SequenceNumber.ShouldEqual(EventSequenceNumber.First);
    [Fact] void should_not_open_sequence_storage() => eventStoreNamespaceStorage.DidNotReceive().GetEventSequence(_key.EventSequenceId);
    [Fact] void should_not_read_event_types() => eventTypesStorage.DidNotReceive().GetLatestForAllEventTypes();
    [Fact] void should_not_read_sequence_state() => eventSequenceStorage.DidNotReceive().GetState();
    [Fact] void should_not_read_the_tail() => eventSequenceStorage.DidNotReceive().GetTailSequenceNumber();
}
