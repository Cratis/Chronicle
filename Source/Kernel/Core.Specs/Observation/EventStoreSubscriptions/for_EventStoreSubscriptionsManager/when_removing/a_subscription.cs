// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.EventStoreSubscriptions;
using Cratis.Chronicle.Namespaces;
using Orleans.TestKit;

namespace Cratis.Chronicle.Observation.EventStoreSubscriptions.for_EventStoreSubscriptionsManager.when_removing;

public class a_subscription : Specification
{
    const string TargetEventStore = "Core";
    const string SourceEventStore = "Lobby";

    TestKitSilo _silo;
    EventStoreSubscriptionsManager _manager;
    IObserver _observer;

    async Task Establish()
    {
        _silo = new TestKitSilo();

        var localSiloDetails = Substitute.For<ILocalSiloDetails>();
        _silo.AddService(localSiloDetails);

        var namespaces = Substitute.For<INamespaces>();
        namespaces.GetAll().Returns([Concepts.EventStoreNamespaceName.Default]);
        _silo.AddProbe(_ => namespaces);

        _observer = Substitute.For<IObserver>();
        _observer.IsSubscribed().Returns(true);
        _silo.AddProbe(_ => _observer);

        _manager = await _silo.CreateGrainAsync<EventStoreSubscriptionsManager>(TargetEventStore);

        await _manager.Add(
            new EventStoreSubscriptionDefinition(
                new EventStoreSubscriptionId(SourceEventStore),
                new Concepts.EventStoreName(SourceEventStore),
                [new Concepts.Events.EventType("5db7cfa2-0fcb-4791-b174-83ff2806d654", Concepts.Events.EventTypeGeneration.First)]));
    }

    async Task Because() => await _manager.Remove(new EventStoreSubscriptionId(SourceEventStore));

    [Fact] void should_unsubscribe_observer() => _observer.Received(1).Unsubscribe();
}
