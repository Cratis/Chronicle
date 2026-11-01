// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.TypeSets.given;

public class a_type_set_store : Specification
{
    protected IStorage _storage;
    protected IEventStoreStorage _eventStore;
    protected IEventStoreNamespaceStorage _namespace;
    protected IObserverStateStorage _observers;

    void Establish()
    {
        _storage = Substitute.For<IStorage>();
        _eventStore = Substitute.For<IEventStoreStorage>();
        _namespace = Substitute.For<IEventStoreNamespaceStorage>();
        _observers = Substitute.For<IObserverStateStorage>();
        _storage.GetEventStore(Arg.Any<EventStoreName>()).Returns(_eventStore);
        _eventStore.GetNamespace(Arg.Any<EventStoreNamespaceName>()).Returns(_namespace);
        _namespace.Observers.Returns(_observers);
    }
}
