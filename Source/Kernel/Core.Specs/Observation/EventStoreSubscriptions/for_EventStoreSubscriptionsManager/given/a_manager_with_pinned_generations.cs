// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Observation.EventStoreSubscriptions;
using Cratis.Chronicle.Namespaces;
using Cratis.Chronicle.Storage.Observation;
using Orleans.TestKit;

namespace Cratis.Chronicle.Observation.EventStoreSubscriptions.for_EventStoreSubscriptionsManager.given;

public class a_manager_with_pinned_generations : Specification
{
    protected EventStoreSubscriptionsManager _manager;
    protected IObserver _observer;
    protected EventGenerationDelivery _currentMode = EventGenerationDelivery.Pinned;
    protected EventType[] _currentPins = [new("customer", 3)];
    protected EventStoreSubscriptionDefinition _definition;

    async Task Establish()
    {
        var silo = new TestKitSilo();
        silo.AddService(Substitute.For<ILocalSiloDetails>());
        var namespaces = Substitute.For<INamespaces>();
        namespaces.GetAll().Returns([EventStoreNamespaceName.Default]);
        silo.AddProbe(_ => namespaces);
        _observer = Substitute.For<IObserver>();
        _observer.IsSubscribed().Returns(true);
        _observer.GetEventTypes().Returns(_ => _currentPins);
        _observer.GetDefinition().Returns(_ => ObserverDefinition.Empty with { GenerationDelivery = _currentMode, EventTypes = _currentPins });
        silo.AddProbe(_ => _observer);
        _manager = await silo.CreateGrainAsync<EventStoreSubscriptionsManager>("Lobby");
        _definition = new("subscription", "Admin", [new EventType("customer", 3)]) { GenerationDelivery = EventGenerationDelivery.Pinned };
        await _manager.Add(_definition);
    }

    protected void ShouldSubscribePinned() => _observer.Received(1).SubscribeWithGenerationDelivery<IEventStoreSubscriptionObserverSubscriber>(
        ObserverType.External, Arg.Is<IEnumerable<EventType>>(pins => pins.Single() == new EventType("customer", 3)), Arg.Any<SiloAddress>(), EventGenerationDelivery.Pinned, "Lobby", true, Arg.Any<ObserverFilters?>(), true, automatic: true);
}
