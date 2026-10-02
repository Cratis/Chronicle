// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Observation.EventStoreSubscriptions;
using Cratis.Chronicle.Namespaces;
using Cratis.Chronicle.Observation.EventStoreSubscriptions;
using Cratis.Chronicle.Observation.States;
using Orleans.TestKit;

namespace Cratis.Chronicle.Observation.for_Observer.when_subscribing;

public class and_automatic_reconciliation_after_restart_is_ready_while_quarantined : given.a_reactivated_quarantined_observer
{
    readonly EventType[] _eventTypes = [EventType.Unknown];
    readonly TestKitSilo _managerSilo = new();
    EventStoreSubscriptionsManager _manager;

    async Task Establish()
    {
        var namespaces = Substitute.For<INamespaces>();
        namespaces.GetAll().Returns([_observerKey.Namespace]);
        _managerSilo.AddService(Substitute.For<ILocalSiloDetails>());
        _managerSilo.AddProbe(_ => namespaces);
        _managerSilo.AddProbe<IObserver>(_ => _observer);
        _manager = await _managerSilo.CreateGrainAsync<EventStoreSubscriptionsManager>("target");
        await _manager.Add(new(new EventStoreSubscriptionId(_observerKey.EventStore.Value), _observerKey.EventStore, _eventTypes));
        (await _observer.IsSubscribed()).ShouldBeFalse();
    }

    async Task Because()
    {
        await _manager.SourceEventStoreAdded(_observerKey.EventStore);
        await _manager.WaitUntilSubscribed(new(_observerKey.EventStore.Value), TimeSpan.Zero);
    }

    [Fact] async Task should_be_ready_without_waiting() => (await _observer.IsSubscribed()).ShouldBeTrue();
    [Fact] async Task should_keep_the_quarantined_state() => (await _observer.GetCurrentState()).ShouldBeOfExactType<QuarantinedObserver>();
    [Fact] void should_keep_the_persisted_quarantine() => _stateStorage.State.RunningState.ShouldEqual(ObserverRunningState.Quarantined);
    [Fact] void should_record_the_event_types() => _definitionStorage.State.EventTypes.ShouldEqual(_eventTypes);
}
