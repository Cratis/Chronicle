// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Observation;
using Orleans.TestKit;

namespace Cratis.Chronicle.Observation.for_Observer.given;

public class a_live_observer_with_guarded_state : an_observer
{
    protected Storage.InMemory.Observation.ObserverStateStorage _guarded;
    protected ObserverDefinition _storedDefinition;

    async Task Establish()
    {
        _guarded = new();
        await _guarded.Save(_stateStorage.State with { Identifier = _observerId }, _observerKey.EventSequenceId);
        await Crash();
        await _observer.SubscribeToAllEvents<NullObserverSubscriber>(ObserverType.Projection, SiloAddress.Zero);
    }

    protected override void ConfigureStorage(TestKitSilo silo, ObserverState persistedState)
    {
        _storedDefinition = _definitionStorage.State with { };
        _eventStoreStorage.Observers.Get(_observerId).Returns(_ => _storedDefinition);
        _eventStoreStorage.Observers.Save(Arg.Any<ObserverDefinition>()).Returns(call =>
        {
            _storedDefinition = call.Arg<ObserverDefinition>() with { };
            return Task.CompletedTask;
        });
        _eventStoreNamespaceStorage.Observers.Returns(_guarded);
        var grainId = GrainId.Create("observer", _observerKey.ToString());
        var definitions = new provider_backed_storage<ObserverDefinition>(new ObserverDefinitionGrainStorageProvider(_storage), grainId);
        var state = new provider_backed_storage<ObserverState>(new ObserverStateGrainStorageProvider(_storage, Microsoft.Extensions.Logging.Abstractions.NullLogger<ObserverStateGrainStorageProvider>.Instance), grainId);
        silo.Options.StorageFactory = type =>
        {
            if (type == typeof(ObserverDefinition)) return definitions;
            if (type == typeof(ObserverState)) return state;
            return null!;
        };
    }

    protected async Task MoveDefinition()
    {
        _storedDefinition = _storedDefinition with { EventSequenceId = (EventSequenceId)"another-sequence" };
        await _guarded.ClaimOwnership(_observerId, _storedDefinition.EventSequenceId);
    }

    void Destroy() => _guarded.Dispose();
}
