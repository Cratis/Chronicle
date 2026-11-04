// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.EventSequences.for_EventSequencesStorageProvider.when_reading_state;

public class and_the_sequence_name_is_custom : given.the_provider
{
    readonly EventSequenceKey _key = new("orders", "event-store", "default");
    readonly GrainState<EventSequenceState> _state = new() { State = new() };

    void Establish()
    {
        eventSequenceStorage.GetState().Returns(new EventSequenceState());
        eventSequenceStorage.GetTailSequenceNumber().Returns(EventSequenceNumber.Unavailable);
    }

    Task Because() => provider.ReadStateAsync("name", GrainId.Create("type", _key.ToString()), _state);

    [Fact] void should_read_sequence_state() => eventSequenceStorage.Received(1).GetState();
    [Fact] void should_not_enumerate_stores() => storage.DidNotReceive().GetEventStores();
    [Fact] void should_not_enumerate_sequences() => eventStoreNamespaceStorage.DidNotReceive().GetEventSequences();
}
