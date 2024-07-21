// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Grains.Jobs;
using Cratis.Chronicle.Jobs;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Keys;
using Orleans.Core;
using Orleans.TestKit;
using IStorage = Cratis.Chronicle.Storage.IStorage;

namespace Cratis.Chronicle.Grains.Observation.Jobs.for_CatchUpObserver.given;

public class a_catchup_observer : Specification
{
    protected TestKitSilo silo = new();
    protected Mock<IStorage> storage;
    protected Mock<IEventStoreStorage> event_store_storage;
    protected Mock<IEventStoreNamespaceStorage> event_store_namespace_storage;
    protected Mock<IObserverKeyIndexes> observer_key_indexes;
    protected CatchUpObserverWrapper job;
    protected JobKey job_key = new(EventStoreName.NotSet, EventStoreNamespaceName.NotSet);
    protected JobId job_id => Guid.Parse("6341bcdb-c644-40ab-81cf-43907c510285");
    protected IStorage<CatchUpObserverState> state_storage;
    protected CatchUpObserverState state => state_storage.State;

    async Task Establish()
    {
        storage = new();
        event_store_storage = new();
        event_store_namespace_storage = new();
        observer_key_indexes = new();
        storage.Setup(_ => _.GetEventStore(IsAny<EventStoreName>())).Returns(event_store_storage.Object);
        event_store_storage.Setup(_ => _.GetNamespace(IsAny<EventStoreNamespaceName>())).Returns(event_store_namespace_storage.Object);
        event_store_namespace_storage.Setup(_ => _.ObserverKeyIndexes).Returns(observer_key_indexes.Object);
        state_storage = silo.StorageManager.GetStorage<CatchUpObserverState>(typeof(Observer).FullName);

        silo.AddService(storage.Object);
        silo.AddService(new JsonSerializerOptions());

        var jobMock = new Mock<ICatchUpObserver>();

        silo.AddProbe(_ => jobMock.Object);
        job = await silo.CreateGrainAsync<CatchUpObserverWrapper>(job_id, job_key);
    }
}
