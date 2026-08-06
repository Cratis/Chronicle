// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Observation.EventStoreSubscriptions;
using Cratis.Chronicle.Namespaces;
using Orleans.TestKit;

namespace Cratis.Chronicle.Observation.EventStoreSubscriptions.for_EventStoreSubscriptionsManager.when_source_event_store_is_added;

public class and_a_subscription_is_already_active_with_the_current_event_types : Specification
{
    const string TargetEventStore = "Lobby";
    const string SourceEventStore = "StudioAdmin";

    TestKitSilo _silo;
    EventStoreSubscriptionsManager _manager;
    IObserver _observer;

    async Task Establish()
    {
        _silo = new TestKitSilo();

        var localSiloDetails = Substitute.For<ILocalSiloDetails>();
        _silo.AddService(localSiloDetails);

        var namespaces = Substitute.For<INamespaces>();
        namespaces.GetAll().Returns([EventStoreNamespaceName.Default]);
        _silo.AddProbe(_ => namespaces);

        var eventType = new EventType("5db7cfa2-0fcb-4791-b174-83ff2806d654", EventTypeGeneration.First);

        _observer = Substitute.For<IObserver>();
        _observer.IsSubscribed().Returns(true);
        _observer.GetEventTypes().Returns([eventType]);
        _silo.AddProbe(_ => _observer);

        _manager = await _silo.CreateGrainAsync<EventStoreSubscriptionsManager>(TargetEventStore);

        await _manager.Add(
            new EventStoreSubscriptionDefinition(
                new EventStoreSubscriptionId(SourceEventStore),
                new EventStoreName(SourceEventStore),
                [eventType]));
    }

    async Task Because() => await _manager.SourceEventStoreAdded(new EventStoreName(SourceEventStore));

    [Fact] void should_not_unsubscribe() => _observer.DidNotReceive().Unsubscribe();

    [Fact] void should_not_subscribe_again() =>
        _observer.DidNotReceive().Subscribe<IEventStoreSubscriptionObserverSubscriber>(
            Arg.Any<ObserverType>(),
            Arg.Any<IEnumerable<EventType>>(),
            Arg.Any<SiloAddress>(),
            Arg.Any<object?>(),
            Arg.Any<bool>());
}
