// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.Sequences.for_ReservedEventSequenceRules.given;

public class a_reserved_name_check : Specification
{
    protected const string EventStore = "some-event-store";
    protected const string Namespace = "some-namespace";

    protected IStorage _storage;
    protected IEventStoreStorage _eventStoreStorage;
    protected IEventStoreNamespaceStorage _namespaceStorage;
    protected IReservedEventSequenceIds _reservedEventSequenceIds;

    void Establish()
    {
        _storage = Substitute.For<IStorage>();
        _eventStoreStorage = Substitute.For<IEventStoreStorage>();
        _namespaceStorage = Substitute.For<IEventStoreNamespaceStorage>();
        _reservedEventSequenceIds = Substitute.For<IReservedEventSequenceIds>();

        _storage.GetEventStores().Returns(Task.FromResult<IEnumerable<EventStoreName>>([(EventStoreName)EventStore]));
        _storage.GetEventStore(Arg.Any<EventStoreName>()).Returns(_eventStoreStorage);
        _eventStoreStorage.GetNamespace(Arg.Any<EventStoreNamespaceName>()).Returns(_namespaceStorage);
        _namespaceStorage.GetEventSequences().Returns(Task.FromResult<IEnumerable<EventSequenceId>>([]));
    }

    protected void Reserve(string eventSequenceId) =>
        _reservedEventSequenceIds.IsReserved(new EventSequenceId(eventSequenceId)).Returns(true);

    protected Task<bool> Check(string eventSequenceId) =>
        Task.FromResult(ReservedEventSequenceRules.MayBeWritten(_reservedEventSequenceIds, EventStore, Namespace, eventSequenceId));
}
