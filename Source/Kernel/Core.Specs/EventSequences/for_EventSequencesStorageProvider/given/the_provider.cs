// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.EventTypes;

namespace Cratis.Chronicle.EventSequences.for_EventSequencesStorageProvider.given;

public class the_provider : Specification
{
    protected EventSequencesStorageProvider provider = null!;
    protected IStorage storage = null!;
    protected IEventStoreStorage eventStoreStorage = null!;
    protected IEventStoreNamespaceStorage eventStoreNamespaceStorage = null!;
    protected IEventTypesStorage eventTypesStorage = null!;
    protected IEventSequenceStorage eventSequenceStorage = null!;

    void Establish()
    {
        storage = Substitute.For<IStorage>();
        eventStoreStorage = Substitute.For<IEventStoreStorage>();
        eventStoreNamespaceStorage = Substitute.For<IEventStoreNamespaceStorage>();
        eventTypesStorage = Substitute.For<IEventTypesStorage>();
        eventSequenceStorage = Substitute.For<IEventSequenceStorage>();
        provider = new(storage);

        storage.GetEventStore(Arg.Any<EventStoreName>()).Returns(eventStoreStorage);
        eventStoreStorage.GetNamespace(Arg.Any<EventStoreNamespaceName>()).Returns(eventStoreNamespaceStorage);
        eventStoreStorage.EventTypes.Returns(eventTypesStorage);
        eventStoreNamespaceStorage.GetEventSequence(Arg.Any<EventSequenceId>()).Returns(eventSequenceStorage);
    }
}
