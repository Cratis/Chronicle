// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.for_ObserverStateGrainStorageProvider.given;

public class the_provider : Specification
{
    protected ObserverStateGrainStorageProvider provider;
    protected IStorage storage;
    protected IEventStoreStorage eventStoreStorage;
    protected IEventStoreNamespaceStorage eventStoreNamespaceStorage;
    protected IObserverStateStorage observerStateStorage;
    protected IFailedPartitionsStorage failedPartitionsStorage;

    void Establish()
    {
        storage = Substitute.For<IStorage>();
        eventStoreStorage = Substitute.For<IEventStoreStorage>();
        eventStoreNamespaceStorage = Substitute.For<IEventStoreNamespaceStorage>();
        observerStateStorage = Substitute.For<IObserverStateStorage>();
        failedPartitionsStorage = Substitute.For<IFailedPartitionsStorage>();
        provider = new(storage);

        eventStoreNamespaceStorage.Observers.Returns(observerStateStorage);
        eventStoreNamespaceStorage.FailedPartitions.Returns(failedPartitionsStorage);
        storage.GetEventStore(Arg.Any<EventStoreName>()).Returns(eventStoreStorage);
        eventStoreStorage.GetNamespace(Arg.Any<EventStoreNamespaceName>()).Returns(eventStoreNamespaceStorage);
        observerStateStorage.Get(Arg.Any<ObserverId>()).Returns(Task.FromResult(new ObserverState()));
        failedPartitionsStorage.GetFor(Arg.Any<ObserverId>()).Returns(Task.FromResult(new FailedPartitions()));
    }
}
