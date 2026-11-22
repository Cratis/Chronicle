// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Reactors.Clients;

namespace Cratis.Chronicle.Observation.for_Observer.when_going_dormant.given;

/// <summary>
/// A client reactor observer that one client registered and a later client, of another replica, replaced with another set of
/// event types: the first client is kept as a standby that takes over when the active one leaves.
/// </summary>
public class a_client_observer_with_a_standby : a_subscribed_client_observer
{
    protected static readonly EventType active_event_type = new("87e51e33-1e9c-4c66-a4a6-3a1372c7ecd6", EventTypeGeneration.First);
    protected ConnectedClient _activeClient;

    async Task Establish()
    {
        _activeClient = new ConnectedClient { ConnectionId = "b-connection", Version = "2.0.0" };
        await _observer.Subscribe<IReactorObserverSubscriber>(ObserverType.Reactor, [active_event_type], SiloAddress.Zero, _activeClient);
        await _silo.TimerRegistry.FireAllAsync();
        await _observer.ReceiveReminder(Observer.AlertReminderName, default);
    }
}
