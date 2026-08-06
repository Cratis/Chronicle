// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Observation.EventStoreSubscriptions;
using Cratis.Chronicle.Namespaces;
using Orleans.TestKit;

namespace Cratis.Chronicle.Observation.EventStoreSubscriptions.for_EventStoreSubscriptionsManager.when_source_event_store_is_added;

/// <summary>
/// Regression for https://github.com/Cratis/Chronicle/issues/3591 — an observer that is already
/// subscribed does not automatically pick up an event type added to the subscription definition
/// after the observer first subscribed, because <c>RefreshSubscription</c> previously returned as
/// soon as <c>IsSubscribed()</c> was true, without comparing the observer's own event types against
/// the current definition.
/// </summary>
public class and_a_subscription_is_active_with_a_newly_introduced_event_type : Specification
{
    const string TargetEventStore = "Lobby";
    const string SourceEventStore = "StudioAdmin";

    TestKitSilo _silo;
    EventStoreSubscriptionsManager _manager;
    IObserver _observer;
    EventType _existingEventType;
    EventType _newlyIntroducedEventType;
    EventType[] _allEventTypes;

    async Task Establish()
    {
        _silo = new TestKitSilo();

        var localSiloDetails = Substitute.For<ILocalSiloDetails>();
        _silo.AddService(localSiloDetails);

        var namespaces = Substitute.For<INamespaces>();
        namespaces.GetAll().Returns([EventStoreNamespaceName.Default]);
        _silo.AddProbe(_ => namespaces);

        _existingEventType = new EventType("5db7cfa2-0fcb-4791-b174-83ff2806d654", EventTypeGeneration.First);
        _newlyIntroducedEventType = new EventType("a883c6c1-0c1a-4b9b-8b3a-6c9e2b6e6f0a", EventTypeGeneration.First);
        _allEventTypes = [_existingEventType, _newlyIntroducedEventType];

        _observer = Substitute.For<IObserver>();
        _observer.IsSubscribed().Returns(true);
        _observer.GetEventTypes().Returns([_existingEventType]);
        _silo.AddProbe(_ => _observer);

        _manager = await _silo.CreateGrainAsync<EventStoreSubscriptionsManager>(TargetEventStore);

        await _manager.Add(
            new EventStoreSubscriptionDefinition(
                new EventStoreSubscriptionId(SourceEventStore),
                new EventStoreName(SourceEventStore),
                _allEventTypes));
    }

    async Task Because() => await _manager.SourceEventStoreAdded(new EventStoreName(SourceEventStore));

    [Fact] void should_not_unsubscribe() => _observer.DidNotReceive().Unsubscribe();

    [Fact]
    void should_subscribe_again_with_the_full_event_type_list() =>
        _observer.Received(1).Subscribe<IEventStoreSubscriptionObserverSubscriber>(
            ObserverType.External,
            Arg.Is<IEnumerable<EventType>>(types => types.SequenceEqual(_allEventTypes)),
            Arg.Any<SiloAddress>(),
            Arg.Any<object?>(),
            Arg.Any<bool>());
}
