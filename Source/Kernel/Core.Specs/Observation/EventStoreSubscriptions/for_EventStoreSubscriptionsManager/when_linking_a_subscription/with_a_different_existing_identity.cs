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

public class with_a_different_existing_identity : Specification
{
    IObserver _existingObserver;
    IObserver _currentObserver;
    EventStoreSubscriptionsManager _manager;
    readonly List<string> _requestedKeys = [];
    readonly ObserverKey _existingKey = new("producer->consumer", "producer", EventStoreNamespaceName.Default, EventSequenceId.Outbox);
    readonly ObserverKey _currentKey = new("explicit->consumer", "producer", EventStoreNamespaceName.Default, EventSequenceId.Outbox);

    async Task Establish()
    {
        var silo = new TestKitSilo();
        silo.AddService(Substitute.For<ILocalSiloDetails>());
        var namespaces = Substitute.For<INamespaces>();
        namespaces.GetAll().Returns([EventStoreNamespaceName.Default]);
        silo.AddProbe(_ => namespaces);
        _existingObserver = Substitute.For<IObserver>();
        _existingObserver.IsSubscribed().Returns(true);
        _existingObserver.ClearReceivedCalls();
        _currentObserver = Substitute.For<IObserver>();
        silo.AddProbe<IObserver>(identity =>
        {
            var key = identity.ToString();
            _requestedKeys.Add(key);
            return key == _existingKey.ToString() ? _existingObserver : _currentObserver;
        });
        _manager = await silo.CreateGrainAsync<EventStoreSubscriptionsManager>("consumer");
        await _manager.Add(new EventStoreSubscriptionDefinition("explicit", "producer", [new EventType("published", 1)]));
    }

    Task Because() => _manager.SourceEventStoreAdded("producer");

    [Fact] void should_request_only_the_current_definition_key() => _requestedKeys.Distinct().ShouldContainOnly([_currentKey.ToString()]);
    [Fact] void should_not_reuse_the_other_observers_progress() => _existingObserver.ReceivedCalls().ShouldBeEmpty();
    [Fact]
    void should_subscribe_the_current_identity() => _currentObserver.Received(1).Subscribe<IEventStoreSubscriptionObserverSubscriber>(
        ObserverType.External,
        Arg.Any<IEnumerable<EventType>>(),
        Arg.Any<SiloAddress>(),
        "consumer",
        Arg.Any<bool>(),
        Arg.Any<ObserverFilters?>(),
        true,
        automatic: true);
}
