// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_subscribing;

/// <summary>
/// Automatic reconciliation after a restart re-attaches persisted observers; it must never bring back a removed namespace.
/// </summary>
public class and_the_namespace_does_not_exist_and_the_subscription_is_automatic : given.an_observer
{
    static readonly EventType _eventType = new("d9a13e10-21a4-4cfc-896e-fda8dfeb79bb", EventTypeGeneration.First);

    void Establish() => _namespaces.GetCanonical(Arg.Any<EventStoreNamespaceName>()).Returns(Task.FromResult<EventStoreNamespaceName?>(null));

    async Task Because() => await _observer.Subscribe<NullObserverSubscriber>(ObserverType.Reactor, [_eventType], SiloAddress.Zero, new ConnectedClient { ConnectionId = "a-connection", Version = "1.0.0" }, automatic: true);

    [Fact] async Task should_not_create_the_namespace() => await _namespaces.DidNotReceive().EnsureCanonical(Arg.Any<EventStoreNamespaceName>());
}
