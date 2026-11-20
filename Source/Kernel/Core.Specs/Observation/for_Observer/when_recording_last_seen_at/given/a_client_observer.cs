// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Clients;
using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Observation.given;
using Cratis.Chronicle.Observation.Reactors.Clients;
using Cratis.Chronicle.Storage.Observation;
using Orleans.TestKit;

namespace Cratis.Chronicle.Observation.for_Observer.when_recording_last_seen_at.given;

/// <summary>
/// A client reactor observer whose clock the spec controls, with the unseen removal at its default, which is off, and its period at seven days.
/// </summary>
public class a_client_observer : for_Observer.given.an_observer_that_lost_its_client
{
    protected readonly DeliveryClock _clock = new();

    public a_client_observer() => _timeProvider = _clock;

    protected IConnectedClients _connectedClients;

    protected virtual TimeSpan UnseenRemovalAfter => TimeSpan.FromDays(7);

    /// <summary>
    /// Gets whether the silo reports the client as connected when the watchdog asks.
    /// </summary>
    protected virtual bool ClientIsConnected => true;

    void Establish()
    {
        _connectedClients = Substitute.For<IConnectedClients>();
        _connectedClients.IsConnected(Arg.Any<ConnectionId>()).Returns(ClientIsConnected);
        _silo.AddProbe(_ => _connectedClients);
    }

    /// <summary>
    /// Gets whether the removal is turned on; it is off by default, and the last-seen time is recorded either way.
    /// </summary>
    protected virtual bool RemovalEnabled => false;

    protected override Observers CreateObserversConfig() => new() { WriteCoalescingInterval = 0, UnseenRemovalEnabled = RemovalEnabled, UnseenRemovalAfter = UnseenRemovalAfter };

    protected Task SubscribeClient() =>
        _observer.Subscribe<IReactorObserverSubscriber>(ObserverType.Reactor, [event_type], SiloAddress.Zero, _client);

    protected int LastSeenSaves =>
        _eventStoreNamespaceStorage.Observers.ReceivedCalls().Count(call => call.GetMethodInfo().Name == nameof(IObserverStateStorage.SaveLastSeenAt));
}
