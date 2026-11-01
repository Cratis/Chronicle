// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Observation;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Observation.for_ObserverStateGrainStorageProvider.given;

public class the_provider : Specification
{
    protected ObserverStateGrainStorageProvider provider;
    protected IStorage storage;
    protected IEventStoreStorage eventStoreStorage;
    protected IEventStoreNamespaceStorage eventStoreNamespaceStorage;
    protected IObserverStateStorage observerStateStorage;
    protected IFailedPartitionsStorage failedPartitionsStorage;
    protected ILogger<ObserverStateGrainStorageProvider> logger;

    void Establish()
    {
        storage = Substitute.For<IStorage>();
        eventStoreStorage = Substitute.For<IEventStoreStorage>();
        eventStoreNamespaceStorage = Substitute.For<IEventStoreNamespaceStorage>();
        observerStateStorage = Substitute.For<IObserverStateStorage>();
        failedPartitionsStorage = Substitute.For<IFailedPartitionsStorage>();
        logger = Substitute.For<ILogger<ObserverStateGrainStorageProvider>>();
        logger.IsEnabled(LogLevel.Error).Returns(true);
        provider = new(storage, logger);
        eventStoreStorage.AreObserversInitialized().Returns(false);

        eventStoreNamespaceStorage.Observers.Returns(observerStateStorage);
        eventStoreNamespaceStorage.FailedPartitions.Returns(failedPartitionsStorage);
        storage.GetEventStore(Arg.Any<EventStoreName>()).Returns(eventStoreStorage);
        eventStoreStorage.GetNamespace(Arg.Any<EventStoreNamespaceName>()).Returns(eventStoreNamespaceStorage);
        observerStateStorage.Get(Arg.Any<ObserverId>()).Returns(Task.FromResult(new ObserverState()));
        observerStateStorage.Save(Arg.Any<ObserverState>(), Arg.Any<Concepts.EventSequences.EventSequenceId>()).Returns(true);
        failedPartitionsStorage.GetFor(Arg.Any<ObserverId>()).Returns(Task.FromResult(new FailedPartitions()));
    }
}
