// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation.Reducers;
using Cratis.Chronicle.Storage;
using Microsoft.Extensions.Logging;
using Orleans.TestKit;

namespace Cratis.Chronicle.Observation.Reducers.Clients.for_ReducersManager.given;

public class a_reducers_manager : Specification
{
    protected ReducersManager _grain;
    protected IEventStoreStorage _eventStoreStorage;
    protected ReducerDefinition _definition;
    protected ReducersManagerState _state;
    protected bool _result;

    async Task Establish()
    {
        var silo = new TestKitSilo();
        silo.AddService(Substitute.For<ILogger<ReducersManager>>());
        var storage = Substitute.For<IStorage>();
        _eventStoreStorage = Substitute.For<IEventStoreStorage>();
        _state = new();
        var stateStorage = Substitute.For<Orleans.Core.IStorage<ReducersManagerState>>();
        stateStorage.State = _state;
        silo.Options.StorageFactory = _ => stateStorage;
        storage.GetEventStore((EventStoreName)"store").Returns(_eventStoreStorage);
        silo.AddService(storage);
        _definition = new("reducer", EventSequenceId.Log, [], "read-model", true, []);
        _grain = await silo.CreateGrainAsync<ReducersManager>("store");
    }
}
