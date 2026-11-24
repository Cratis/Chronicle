// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage;
using Orleans.TestKit;

namespace Cratis.Chronicle.Observation.Reducers.Clients.for_ReducersManager.given;

public class a_reducers_manager : Specification
{
    protected ReducersManager _grain;
    protected IEventStoreStorage _eventStoreStorage;
    protected bool _result;

    async Task Establish()
    {
        var silo = new TestKitSilo();
        var storage = Substitute.For<IStorage>();
        _eventStoreStorage = Substitute.For<IEventStoreStorage>();
        storage.GetEventStore((EventStoreName)"store").Returns(_eventStoreStorage);
        silo.AddService(storage);
        _grain = await silo.CreateGrainAsync<ReducersManager>("store");
    }
}
