// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Observation.EventStoreSubscriptions;
using Cratis.Chronicle.Namespaces;
using Cratis.Chronicle.Storage.Observation;
using Orleans.TestKit;

namespace Cratis.Chronicle.Observation.EventStoreSubscriptions.for_EventStoreSubscriptionsManager.when_receiving_subscription_reminder;

public class and_the_observer_holds_stale_event_types : Specification
{
    const string TargetEventStore = "Lobby";
    const string SourceEventStore = "StudioAdmin";
    const string ReminderName = "event-store-subscription-subscribe:StudioAdmin";

    TestKitSilo _silo;
    EventStoreSubscriptionsManager _manager;
    IObserver _observer;
    EventType _eventType;

    async Task Establish()
    {
        _silo = new TestKitSilo();
        _silo.AddNamespaceCasing();

        var localSiloDetails = Substitute.For<ILocalSiloDetails>();
        _silo.AddService(localSiloDetails);

        var namespaces = Substitute.For<INamespaces>();
        namespaces.GetAll().Returns([EventStoreNamespaceName.Default]);
        _silo.AddProbe(_ => namespaces);

        _eventType = new EventType("5db7cfa2-0fcb-4791-b174-83ff2806d654", EventTypeGeneration.First);

        _observer = Substitute.For<IObserver>();
        _observer.IsSubscribed().Returns(true);
        var staleEventType = new EventType("0b3a0b0e-3a1f-4c7d-9d4a-5f1a7e9c2b11", EventTypeGeneration.First);
        _observer.GetDefinition().Returns(ObserverDefinition.Empty with { EventTypes = [staleEventType] });
        _observer.GetEventTypes().Returns([staleEventType]);
        _silo.AddProbe(_ => _observer);

        _manager = await _silo.CreateGrainAsync<EventStoreSubscriptionsManager>(TargetEventStore);

        await _manager.Add(
            new EventStoreSubscriptionDefinition(
                new EventStoreSubscriptionId(SourceEventStore),
                new EventStoreName(SourceEventStore),
                [_eventType]));
    }

    async Task Because() => await _manager.ReceiveReminder(ReminderName, default);

    [Fact]
    void should_subscribe_with_the_current_event_types() =>
        _observer.Received(1).Subscribe<IEventStoreSubscriptionObserverSubscriber>(
            ObserverType.External,
            Arg.Is<IEnumerable<EventType>>(eventTypes => eventTypes.SequenceEqual(new[] { _eventType })),
            Arg.Any<SiloAddress>(),
            Arg.Any<object?>(),
            Arg.Any<bool>(),
            Arg.Any<ObserverFilters?>(),
            true,
            automatic: true);
}
