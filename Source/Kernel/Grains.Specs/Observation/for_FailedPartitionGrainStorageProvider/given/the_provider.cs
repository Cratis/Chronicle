// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Observation;
namespace Cratis.Chronicle.Grains.Observation.for_FailedPartitionGrainStorageProvider.given;

public class the_provider : Specification
{
    protected FailedPartitionGrainStorageProvider provider;
    protected IStorage storage;
    protected IEventStoreStorage eventStoreStorage;
    protected IEventStoreNamespaceStorage eventStoreNamespaceStorage;
    protected IFailedPartitionsStorage _failedPartitionsStorage;

    void Establish()
    {
        storage = Substitute.For<IStorage>();
        eventStoreStorage = Substitute.For<IEventStoreStorage>();
        eventStoreNamespaceStorage = Substitute.For<IEventStoreNamespaceStorage>();
        _failedPartitionsStorage = Substitute.For<IFailedPartitionsStorage>();
        provider = new(storage);

        eventStoreNamespaceStorage.FailedPartitions.Returns(_failedPartitionsStorage);
        storage.GetEventStore(Arg.Any<EventStoreName>()).Returns(eventStoreStorage);
        eventStoreStorage.GetNamespace(Arg.Any<EventStoreNamespaceName>()).Returns(eventStoreNamespaceStorage);
        _failedPartitionsStorage.GetFor(Arg.Any<ObserverId>()).Returns(Task.FromResult(new FailedPartitions()));
    }
}
