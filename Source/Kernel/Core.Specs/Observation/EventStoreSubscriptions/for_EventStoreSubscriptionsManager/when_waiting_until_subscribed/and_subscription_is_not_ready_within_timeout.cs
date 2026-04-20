// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation.EventStoreSubscriptions;
using Cratis.Chronicle.Namespaces;
using Orleans.TestKit;

namespace Cratis.Chronicle.Observation.EventStoreSubscriptions.for_EventStoreSubscriptionsManager.when_waiting_until_subscribed;

public class and_subscription_is_not_ready_within_timeout : Specification
{
    const string TargetEventStore = "Lobby";
    const string SourceEventStore = "StudioAdmin";

    TestKitSilo _silo;
    EventStoreSubscriptionsManager _manager;
    Exception _error;

    async Task Establish()
    {
        _silo = new TestKitSilo();

        var localSiloDetails = Substitute.For<ILocalSiloDetails>();
        _silo.AddService(localSiloDetails);

        var namespaces = Substitute.For<INamespaces>();
        namespaces.GetAll().Returns([EventStoreNamespaceName.Default]);
        _silo.AddProbe(_ => namespaces);

        var observer = Substitute.For<IObserver>();
        observer.IsSubscribed().Returns(false);
        _silo.AddProbe(_ => observer);

        _manager = await _silo.CreateGrainAsync<EventStoreSubscriptionsManager>(TargetEventStore);

        await _manager.Add(
            new EventStoreSubscriptionDefinition(
                new EventStoreSubscriptionId(SourceEventStore),
                new EventStoreName(SourceEventStore),
                [new EventType("5db7cfa2-0fcb-4791-b174-83ff2806d654", EventTypeGeneration.First)]));
    }

    async Task Because() =>
        _error = await Catch.Exception(() =>
            _manager.WaitUntilSubscribed(new EventStoreSubscriptionId(SourceEventStore), TimeSpan.FromMilliseconds(30)));

    [Fact] void should_throw_timeout_exception() => _error.ShouldBeOfExactType<TimeoutException>();
}