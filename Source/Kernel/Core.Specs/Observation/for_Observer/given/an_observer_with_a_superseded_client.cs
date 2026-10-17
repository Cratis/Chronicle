// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Clients;
using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Orleans.TestKit;

namespace Cratis.Chronicle.Observation.for_Observer.given;

/// <summary>
/// An observer a client registered with one set of event types, and a later client replaced with another set.
/// </summary>
public class an_observer_with_a_superseded_client : an_observer
{
    protected static readonly EventType superseded_event_type = new("d9a13e10-21a4-4cfc-896e-fda8dfeb79bb", EventTypeGeneration.First);
    protected static readonly EventType active_event_type = new("87e51e33-1e9c-4c66-a4a6-3a1372c7ecd6", EventTypeGeneration.First);
    protected ConnectedClient _supersededClient;
    protected ConnectedClient _activeClient;
    protected IConnectedClients _connectedClients;

    async Task Establish()
    {
        _supersededClient = new ConnectedClient { ConnectionId = "a-connection", Version = "1.0.0" };
        _activeClient = new ConnectedClient { ConnectionId = "b-connection", Version = "2.0.0" };
        _connectedClients = Substitute.For<IConnectedClients>();
        _silo.AddProbe(_ => _connectedClients);

        await _observer.Subscribe<IClientOwnedObserverSubscriber>(ObserverType.Reactor, [superseded_event_type], SiloAddress.Zero, _supersededClient);
        await _observer.Subscribe<IClientOwnedObserverSubscriber>(ObserverType.Reactor, [active_event_type], SiloAddress.Zero, _activeClient);
        _storageStats.ResetCounts();
    }
}
