// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Observation.EventStoreSubscriptions;
using Cratis.Chronicle.Namespaces;
using Orleans.TestKit;

namespace Cratis.Chronicle.Observation.EventStoreSubscriptions.for_EventStoreSubscriptionsManager.when_linking_a_subscription;

public class with_explicit_scope : Specification
{
    const string SubscriptionId = "orders->archive";
    const string Source = "producer";
    readonly Dictionary<string, IObserver> _observers = [];
    readonly EventStoreNamespaceName[] _namespaces = [new("north"), new("south")];
    readonly string[] _consumers = ["consumer-one", "consumer-two"];
    readonly List<EventStoreSubscriptionsManager> _managers = [];

    async Task Establish()
    {
        foreach (var consumer in _consumers)
        {
            var silo = new TestKitSilo();
            silo.AddNamespaceCasing();
            silo.AddService(Substitute.For<ILocalSiloDetails>());
            var namespaces = Substitute.For<INamespaces>();
            namespaces.GetAll().Returns(_namespaces);
            silo.AddProbe(_ => namespaces);
            silo.AddProbe<IObserver>(identity => GetObserver(identity.ToString()));
            var manager = await silo.CreateGrainAsync<EventStoreSubscriptionsManager>(consumer);
            await manager.Add(new EventStoreSubscriptionDefinition(SubscriptionId, Source, [new EventType("published", 1)]));
            _managers.Add(manager);
        }
    }

    async Task Because()
    {
        foreach (var manager in _managers)
        {
            await manager.SourceEventStoreAdded(Source);
        }
    }

    [Fact] void should_create_four_separate_observers() => _observers.Count.ShouldEqual(4);

    [Fact]
    void should_use_the_explicit_identifier_source_namespace_and_outbox() =>
        _observers.Keys.ShouldContainOnly(_consumers.SelectMany(consumer => _namespaces.Select(ns =>
            new ObserverKey($"{SubscriptionId}->{consumer}", Source, ns, EventSequenceId.Outbox).ToString())));

    [Fact]
    void should_pass_each_consumer_as_the_forwarding_target()
    {
        foreach (var consumer in _consumers)
        {
            foreach (var ns in _namespaces)
            {
                var key = new ObserverKey($"{SubscriptionId}->{consumer}", Source, ns, EventSequenceId.Outbox);
                _observers[key].Received(1).Subscribe<IEventStoreSubscriptionObserverSubscriber>(
                    ObserverType.External,
                    Arg.Any<IEnumerable<EventType>>(),
                    Arg.Any<SiloAddress>(),
                    consumer,
                    Arg.Any<bool>(),
                    Arg.Any<ObserverFilters?>(),
                    true,
                    automatic: true);
            }
        }
    }

    IObserver GetObserver(string key)
    {
        if (!_observers.TryGetValue(key, out var observer))
        {
            observer = Substitute.For<IObserver>();
            _observers.Add(key, observer);
        }
        return observer;
    }
}
