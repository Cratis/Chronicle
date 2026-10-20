// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Namespaces;

namespace Cratis.Chronicle.Observation.for_Observer.when_restoring_client_subscription;

public class and_the_observer_is_quarantined : given.a_quarantined_observer
{
    static readonly EventType _eventType = new("d9a13e10-21a4-4cfc-896e-fda8dfeb79bb", EventTypeGeneration.First);
    readonly ConnectedClient _client = new() { ConnectionId = "a-connection", Version = "1.0.0" };

    void Establish()
    {
        var namespaces = Substitute.For<INamespaceStorage>();
        namespaces.GetAll().Returns([new NamespaceState(_observerKey.Namespace, DateTimeOffset.UtcNow)]);
        _eventStoreStorage.Namespaces.Returns(namespaces);
    }

    Task Because() => _observer.RestoreClientSubscription<NullObserverSubscriber>(ObserverType.Reactor, [_eventType], SiloAddress.Zero, _client);

    [Fact] async Task should_stay_quarantined() => (await _observer.IsObserverQuarantined()).ShouldBeTrue();
    [Fact] async Task should_not_know_the_client() => (await _observer.KnowsClient(_client.ConnectionId)).ShouldBeFalse();
    [Fact] void should_not_have_resubscribed() => ShouldNotHaveResubscribed();
}
