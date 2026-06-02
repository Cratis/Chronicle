// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Services.Observation.for_Observers.given;

public class all_dependencies : Specification
{
    protected IStorage _storage;
    protected IEventStoreStorage _eventStoreStorage;
    protected IEventStoreNamespaceStorage _namespaceStorage;
    protected IObserverDefinitionsStorage _observerDefinitionsStorage;
    protected IObserverStateStorage _observerStateStorage;
    protected IFailedPartitionsStorage _failedPartitionsStorage;
    protected IGrainFactory _grainFactory;
    protected Contracts.Observation.IObservers _observers;

    void Establish()
    {
        _storage = Substitute.For<IStorage>();
        _eventStoreStorage = Substitute.For<IEventStoreStorage>();
        _namespaceStorage = Substitute.For<IEventStoreNamespaceStorage>();
        _observerDefinitionsStorage = Substitute.For<IObserverDefinitionsStorage>();
        _observerStateStorage = Substitute.For<IObserverStateStorage>();
        _failedPartitionsStorage = Substitute.For<IFailedPartitionsStorage>();
        _grainFactory = Substitute.For<IGrainFactory>();

        _storage.GetEventStore(Arg.Any<Concepts.EventStoreName>()).Returns(_eventStoreStorage);
        _eventStoreStorage.Observers.Returns(_observerDefinitionsStorage);
        _eventStoreStorage.GetNamespace(Arg.Any<Concepts.EventStoreNamespaceName>()).Returns(_namespaceStorage);
        _namespaceStorage.Observers.Returns(_observerStateStorage);
        _namespaceStorage.FailedPartitions.Returns(_failedPartitionsStorage);

        _observers = new Observers(_grainFactory, _storage);
    }
}
