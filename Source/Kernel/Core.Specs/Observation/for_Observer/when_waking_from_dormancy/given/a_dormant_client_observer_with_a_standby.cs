// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Reactors.Clients;

namespace Cratis.Chronicle.Observation.for_Observer.when_waking_from_dormancy.given;

/// <summary>
/// A client reactor observer released as dormant while a client of another replica held the subscription and the client that
/// registered first was its standby.
/// </summary>
public class a_dormant_client_observer_with_a_standby : a_dormant_client_observer
{
    protected static readonly EventType active_event_type = new("87e51e33-1e9c-4c66-a4a6-3a1372c7ecd6", EventTypeGeneration.First);
    protected ConnectedClient _activeClient;

    protected override async Task BeforeGoingDormant()
    {
        _activeClient = new ConnectedClient { ConnectionId = "b-connection", Version = "2.0.0" };
        _connectedClients.GetConnectedClient(_client.ConnectionId).Returns(_client);
        _connectedClients.GetConnectedClient(_activeClient.ConnectionId).Returns(_activeClient);
        await _observer.Subscribe<IReactorObserverSubscriber>(ObserverType.Reactor, [active_event_type], SiloAddress.Zero, _activeClient);
        await _silo.TimerRegistry.FireAllAsync();
        await _observer.ReceiveReminder(Observer.AlertReminderName, default);
    }
}
