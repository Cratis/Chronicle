// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.Sequences.for_EventSequenceTail.when_getting_the_tail.given;

public class a_tail_reader : Specification
{
    protected IEventSequenceStorage _eventSequence;
    protected IStorage _storage;

    void Establish()
    {
        _storage = Substitute.For<IStorage>();
        var eventStore = Substitute.For<IEventStoreStorage>();
        var eventStoreNamespace = Substitute.For<IEventStoreNamespaceStorage>();
        _eventSequence = Substitute.For<IEventSequenceStorage>();
        _storage.GetEventStore(Arg.Any<EventStoreName>()).Returns(eventStore);
        eventStore.GetNamespace(Arg.Any<EventStoreNamespaceName>()).Returns(eventStoreNamespace);
        eventStoreNamespace.GetEventSequence(Arg.Any<EventSequenceId>()).Returns(_eventSequence);
        _eventSequence.GetTailSequenceNumber(Arg.Any<IEnumerable<Concepts.Events.EventType>>(), Arg.Any<Concepts.Events.EventSourceId?>(), Arg.Any<Concepts.Events.EventSourceType?>(), Arg.Any<Concepts.Events.EventStreamId?>(), Arg.Any<Concepts.Events.EventStreamType?>()).Returns(10UL);
        _eventSequence.GetState().Returns(new EventSequenceState { MutationEpoch = new EventSequenceMutationEpoch(6) });
    }
}
