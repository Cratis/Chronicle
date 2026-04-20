// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Observation.EventStoreSubscriptions;
using Cratis.Chronicle.Namespaces;
using Orleans.TestKit;

namespace Cratis.Chronicle.Observation.EventStoreSubscriptions.for_EventStoreSubscriptionsManager.when_source_event_store_is_added;

public class and_subscriptions_are_waiting_for_it : Specification
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

        _observer = Substitute.For<IObserver>();
        _observer.IsSubscribed().Returns(false);
        _silo.AddProbe(_ => _observer);

        _manager = await _silo.CreateGrainAsync<EventStoreSubscriptionsManager>(TargetEventStore);

        await _manager.Add(
            new EventStoreSubscriptionDefinition(
                new EventStoreSubscriptionId(SourceEventStore),
                new EventStoreName(SourceEventStore),
                [new EventType("5db7cfa2-0fcb-4791-b174-83ff2806d654", EventTypeGeneration.First)]));

        await _manager.Add(
            new EventStoreSubscriptionDefinition(
                new EventStoreSubscriptionId("another-source"),
                new EventStoreName("another-source"),
                [new EventType("f0034052-e036-4b1b-9fd1-1aaf8f8d4eb4", EventTypeGeneration.First)]));
    }

    async Task Because() => await _manager.SourceEventStoreAdded(new EventStoreName(SourceEventStore));

    [Fact] void should_subscribe_only_matching_pending_subscriptions() =>
        _observer.Received(1).Subscribe<IEventStoreSubscriptionObserverSubscriber>(
            ObserverType.External,
            Arg.Any<IEnumerable<EventType>>(),
            Arg.Any<SiloAddress>(),
            Arg.Is<object?>(metadata => metadata is string && (string)metadata == TargetEventStore),
            Arg.Any<bool>());
}
