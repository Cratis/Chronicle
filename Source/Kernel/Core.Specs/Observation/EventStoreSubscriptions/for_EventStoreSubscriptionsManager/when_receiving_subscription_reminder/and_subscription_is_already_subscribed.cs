// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Observation.EventStoreSubscriptions;
using Cratis.Chronicle.Namespaces;
using Orleans.TestKit;

namespace Cratis.Chronicle.Observation.EventStoreSubscriptions.for_EventStoreSubscriptionsManager.when_receiving_subscription_reminder;

public class and_subscription_is_already_subscribed : Specification
{
    const string TargetEventStore = "Lobby";
    const string SourceEventStore = "StudioAdmin";
    const string ReminderName = "event-store-subscription-subscribe:StudioAdmin";

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
        _observer.IsSubscribed().Returns(true);
        _silo.AddProbe(_ => _observer);

        _manager = await _silo.CreateGrainAsync<EventStoreSubscriptionsManager>(TargetEventStore);

        await _manager.Add(
            new EventStoreSubscriptionDefinition(
                new EventStoreSubscriptionId(SourceEventStore),
                new EventStoreName(SourceEventStore),
                [new EventType("5db7cfa2-0fcb-4791-b174-83ff2806d654", EventTypeGeneration.First)]));
    }

    async Task Because() => await _manager.ReceiveReminder(ReminderName, default);

    [Fact] void should_unsubscribe_before_subscribing_again() => _observer.Received(1).Unsubscribe();

    [Fact] void should_pass_target_event_store_name_as_subscriber_metadata() =>
        _observer.Received(1).Subscribe<IEventStoreSubscriptionObserverSubscriber>(
            ObserverType.External,
            Arg.Any<IEnumerable<EventType>>(),
            Arg.Any<SiloAddress>(),
            Arg.Is<object?>(metadata => metadata is string && (string)metadata == TargetEventStore),
            Arg.Any<bool>());
}
