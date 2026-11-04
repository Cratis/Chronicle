// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.EventSequences.for_EventSequencesStorageProvider.when_reading_state;

public class and_the_sequence_name_is_built_in : given.the_provider
{
    readonly GrainState<EventSequenceState> _state = new() { State = new() };

    void Establish()
    {
        reservedEventSequenceIds.IsReserved(Arg.Any<EventSequenceId>()).Returns(true);
        eventSequenceStorage.GetState().Returns(new EventSequenceState());
        eventSequenceStorage.GetTailSequenceNumber().Returns(EventSequenceNumber.Unavailable);
    }

    async Task Because()
    {
        foreach (var id in new[] { EventSequenceId.Log, EventSequenceId.System, EventSequenceId.Outbox, EventSequenceId.Inbox })
        {
            var key = new EventSequenceKey(id, "event-store", "default");
            await provider.ReadStateAsync("name", GrainId.Create("type", key.ToString()), _state);
        }
    }

    [Fact] void should_read_every_sequence_state() => eventSequenceStorage.Received(4).GetState();
    [Fact] void should_not_consult_the_provider() => reservedEventSequenceIds.DidNotReceive().IsReserved(Arg.Any<EventSequenceId>());
    [Fact] void should_not_enumerate_stores() => storage.DidNotReceive().GetEventStores();
    [Fact] void should_not_enumerate_sequences() => eventStoreNamespaceStorage.DidNotReceive().GetEventSequences();
}
