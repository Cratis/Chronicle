// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Clients;
using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Observation.Reactors.Clients;
using Cratis.Chronicle.Storage.Observation;
using Orleans.TestKit;

namespace Cratis.Chronicle.Observation.for_Observer.when_recording_a_durable_subscription.given;

/// <summary>
/// A client reactor observer whose durable subscription mode the spec chooses, shadow by default.
/// </summary>
public class a_client_observer : for_Observer.given.an_observer_that_lost_its_client
{
    protected IConnectedClients _connectedClients;

    protected virtual DurableSubscriptionMode Mode => DurableSubscriptionMode.Shadow;

    protected ObserverDurableSubscription? Recorded => _stateStorage.State.DurableSubscription;

    void Establish()
    {
        _connectedClients = Substitute.For<IConnectedClients>();
        _connectedClients.IsConnected(Arg.Any<ConnectionId>()).Returns(true);
        _silo.AddProbe(_ => _connectedClients);
    }

    protected override Observers CreateObserversConfig() => new() { WriteCoalescingInterval = 0, DurableSubscriptions = Mode };

    protected Task SubscribeClient() =>
        _observer.Subscribe<IReactorObserverSubscriber>(ObserverType.Reactor, [event_type], SiloAddress.Zero, _client);
}
