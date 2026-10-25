// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Generations.Bindings;
using Cratis.Chronicle.Storage.InMemory.Observation;

namespace Cratis.Chronicle.Storage.InMemory.Generations.Bindings;

public sealed class InMemoryMaterializationBindingStorageHarness : IMaterializationBindingStorageHarness
{
    readonly ObserverDefinitionsStorage _definitions = new();

    public InMemoryMaterializationBindingStorageHarness()
    {
        var eventStore = Substitute.For<IEventStoreStorage>();
        eventStore.Observers.Returns(_definitions);
        var storage = Substitute.For<IStorage>();
        storage.GetEventStore(Arg.Any<EventStoreName>()).Returns(eventStore);
        Claims = new InMemoryObserverIdentityClaims(storage);
    }

    public IObserverIdentityClaims Claims { get; }
    public IMaterializationBindingStorage Create(TimeProvider? timeProvider = null) => new InMemoryMaterializationBindingStorage(timeProvider, Claims);
    public Task SaveObserverDefinition(EventStoreName store, ObserverId observerId) => _definitions.Save(new Cratis.Chronicle.Storage.Observation.ObserverDefinition { Identifier = observerId });
    public void Dispose() { }
}
