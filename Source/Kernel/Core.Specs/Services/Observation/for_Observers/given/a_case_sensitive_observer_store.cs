// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Clients;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Namespaces;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.InMemory;
using Cratis.Chronicle.Storage.Observation;
using Cratis.Chronicle.Storage.Sinks;
using Cratis.Types;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Services.Observation.for_Observers.given;

public class a_case_sensitive_observer_store : for_QueryExecutor.given.a_routed_query
{
    protected static readonly DateTimeOffset _changedAt = new(2026, 1, 2, 3, 4, 5, TimeSpan.Zero);
    protected static readonly EventType _eventType = new("event-type", 1);

    protected Observers _service;
    protected IStorage _storage;
    protected IEventStoreNamespaceStorage _canonicalStorage;
    protected IGrainFactory _grains;
    protected INamespaceCasing _casing;
    protected IObserver _observer;
    protected ObserverDefinition _definition;
    protected ObserverState _state;
    EventStoreStorages _stores;

    async Task Establish()
    {
        _stores = new(new KnownInstancesOf<ISinkFactory>([]), Substitute.For<Cratis.Choreography.Storage.IJobsStorage>());
        _storage = new Storage.InMemory.Storage(_stores, Substitute.For<ISystemStorage>());
        await _storage.GetEventStore("store").Namespaces.Ensure("Cratis");
        _canonicalStorage = _storage.GetEventStore("store").GetNamespace("Cratis");
        _definition = new("observer", [_eventType], EventSequenceId.Log, ObserverType.Reactor, ObserverOwner.Client, true);
        _state = ObserverState.Empty with { Identifier = "observer", NextEventSequenceNumber = 42 };
        await _storage.GetEventStore("store").Observers.Save(_definition);
        await _canonicalStorage.Observers.Save(_state, EventSequenceId.Log);
        await _canonicalStorage.Observers.SaveRunningStateChangedAt("observer", _changedAt, EventSequenceId.Log);

        _grains = Substitute.For<IGrainFactory>();
        var namespaces = Substitute.For<INamespaces>();
        namespaces.GetCanonical(Arg.Any<EventStoreNamespaceName>()).Returns((EventStoreNamespaceName)"Cratis");
        _grains.GetGrain<INamespaces>(Arg.Any<string>(), Arg.Any<string>()).Returns(namespaces);
        _casing = new NamespaceCasing(_grains, NullLogger<NamespaceCasing>.Instance);
        _observer = Substitute.For<IObserver>();
        _observer.GetDefinition().Returns(_definition);
        _observer.GetState().Returns(_state);
        _grains.GetGrain<IObserver>(Arg.Any<string>(), Arg.Any<string>()).Returns(_observer);
        _service = CreateService(_casing);
    }

    protected Observers CreateService(INamespaceCasing casing) =>
        new(_grains, _storage, Substitute.For<IObserverRemover>(), Substitute.For<IConnectedClientsVisibility>(), Options.Create(new ChronicleOptions()), casing);

    void Destroy() => _stores.Dispose();
}
