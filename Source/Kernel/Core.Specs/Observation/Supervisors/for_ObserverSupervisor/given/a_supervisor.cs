// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.ObserverRouting;
using Cratis.Chronicle.Storage;
using Orleans.TestKit;

namespace Cratis.Chronicle.Observation.Supervisors.for_ObserverSupervisor.given;

public class a_supervisor : Specification
{
    protected static readonly EventStoreName _eventStore = "an-event-store";
    protected static readonly ObserverId _observerId = "an-observer";

    protected TestKitSilo _silo = new();
    protected IObserverDefinitionsStateProvider _stateProvider;
    protected IObserverRoutingClient _routingClient;
    protected IObservers _observers;
    protected IObserverBehindCheck _behindCheck;
    protected IStorage _storage;
    protected ObserverSupervisor _supervisor;

    async Task Establish()
    {
        _stateProvider = Substitute.For<IObserverDefinitionsStateProvider>();
        _routingClient = Substitute.For<IObserverRoutingClient>();
        _observers = Substitute.For<IObservers>();
        _behindCheck = Substitute.For<IObserverBehindCheck>();
        _storage = Substitute.For<IStorage>();
        _silo.AddService(_stateProvider);
        _silo.AddService(_routingClient);
        _silo.AddService(_observers);
        _silo.AddService(_behindCheck);
        _silo.AddService(_storage);
        _supervisor = await _silo.CreateGrainAsync<ObserverSupervisor>(new ObserverSupervisorKey(_eventStore, _observerId).ToString());
    }
}
