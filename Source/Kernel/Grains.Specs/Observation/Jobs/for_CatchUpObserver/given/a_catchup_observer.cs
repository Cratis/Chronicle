// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Jobs;
using Cratis.Chronicle.Grains.Jobs;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Keys;
using Orleans.Core;
using Orleans.TestKit;
using IStorage = Cratis.Chronicle.Storage.IStorage;

namespace Cratis.Chronicle.Grains.Observation.Jobs.for_CatchUpObserver.given;

public class a_catchup_observer : Specification
{
    protected TestKitSilo _silo = new();
    protected IStorage _storage;
    protected IEventStoreStorage _eventStoreStorage;
    protected IEventStoreNamespaceStorage _eventStoreNamespaceStorage;
    protected IObserverKeyIndexes _observerKeyIndexes;
    protected CatchUpObserverWrapper _job;
    protected JobKey _jobKey = new(EventStoreName.NotSet, EventStoreNamespaceName.NotSet);
    protected JobId _jobId => Guid.Parse("6341bcdb-c644-40ab-81cf-43907c510285");
    protected IStorage<CatchUpObserverState> _stateStorage;
    protected CatchUpObserverState _state => _stateStorage.State;

    async Task Establish()
    {
        _storage = Substitute.For<IStorage>();
        _eventStoreStorage = Substitute.For<IEventStoreStorage>();
        _eventStoreNamespaceStorage = Substitute.For<IEventStoreNamespaceStorage>();
        _observerKeyIndexes = Substitute.For<IObserverKeyIndexes>();
        _storage.GetEventStore(Arg.Any<EventStoreName>()).Returns(_eventStoreStorage);
        _eventStoreStorage.GetNamespace(Arg.Any<EventStoreNamespaceName>()).Returns(_eventStoreNamespaceStorage);
        _eventStoreNamespaceStorage.ObserverKeyIndexes.Returns(_observerKeyIndexes);
        _stateStorage = _silo.StorageManager.GetStorage<CatchUpObserverState>(typeof(CatchUpObserverWrapper).FullName);

        _silo.AddService(_storage);
        _silo.AddService(new JsonSerializerOptions());

        var jobMock = Substitute.For<IJob>();
        _silo.AddProbe(_ => jobMock);
        _job = await _silo.CreateGrainAsync<CatchUpObserverWrapper>(_jobId, _jobKey);
    }
}
