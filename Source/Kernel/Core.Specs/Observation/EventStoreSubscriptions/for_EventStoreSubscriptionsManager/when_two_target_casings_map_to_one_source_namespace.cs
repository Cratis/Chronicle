// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Observation.EventStoreSubscriptions;
using Cratis.Chronicle.Namespaces;
using Orleans.TestKit;

namespace Cratis.Chronicle.Observation.EventStoreSubscriptions.for_EventStoreSubscriptionsManager;

public class when_two_target_casings_map_to_one_source_namespace : Specification
{
    IObserver _observer;
    EventStoreSubscriptionsManager _manager;
    readonly List<string> _keys = [];

    async Task Establish()
    {
        var silo = new TestKitSilo();
        var casing = silo.AddNamespaceCasing();
        casing.Canonicalize("Source", Arg.Any<EventStoreNamespaceName>()).Returns((EventStoreNamespaceName)"Cratis");
        silo.AddService(Substitute.For<ILocalSiloDetails>());
        var namespaces = Substitute.For<INamespaces>();
        namespaces.GetAll().Returns([(EventStoreNamespaceName)"Cratis", (EventStoreNamespaceName)"cratis"]);
        silo.AddProbe(_ => namespaces);
        _observer = Substitute.For<IObserver>();
        silo.AddProbe<IObserver>(identity =>
        {
            _keys.Add(identity.ToString());
            return _observer;
        });
        _manager = await silo.CreateGrainAsync<EventStoreSubscriptionsManager>("Target");
    }

    async Task Because()
    {
        await _manager.Add(new(new EventStoreSubscriptionId("Source"), "Source", [EventType.Unknown]));
        await _manager.SourceEventStoreAdded("Source");
    }

    [Fact] void should_request_only_the_canonical_observer_key() => _keys.Distinct().ShouldContainOnly(new ObserverKey("Source->Target", "Source", "Cratis", Concepts.EventSequences.EventSequenceId.Outbox).ToString());
    [Fact]
    void should_subscribe_exactly_once() => _observer.Received(1).Subscribe<IEventStoreSubscriptionObserverSubscriber>(
        ObserverType.External, Arg.Any<IEnumerable<EventType>>(), Arg.Any<SiloAddress>(), Arg.Any<object?>(), Arg.Any<bool>(), Arg.Any<ObserverFilters?>(), true, automatic: true);
}
