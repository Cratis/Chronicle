// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Clients;
using Aksio.Cratis.Events;
using Aksio.Cratis.Kernel.Grains.Observation;
using Aksio.Cratis.Kernel.Orleans.Observers;
using Aksio.Cratis.Observation;
using Microsoft.Extensions.Logging;
using Orleans;

namespace Aksio.Cratis.Kernel.Grains.Clients;

/// <summary>
/// Represents an implementation of <see cref="IClientObserver"/>.
/// </summary>
public class ClientObserver : Grain, IClientObserver, INotifyClientDisconnected
{
    readonly ObserverManager<INotifyClientObserverDisconnected> _clientObserverDisconnectedObservers;
    private ConnectionId _connectionId = ConnectionId.NotSet;

    public ClientObserver(ILogger<ClientObserver> logger)
    {
        _clientObserverDisconnectedObservers = new(TimeSpan.FromMinutes(1), logger, "ClientObserverDisconnectedObservers");
    }

    /// <inheritdoc/>
    public async Task Start(ObserverName name, ConnectionId connectionId, IEnumerable<EventType> eventTypes)
    {
        var id = this.GetPrimaryKey(out var keyAsString);
        var key = ObserverKey.Parse(keyAsString);
        var observer = GrainFactory.GetGrain<IObserver>(id, key);
        var connectedClients = GrainFactory.GetGrain<IConnectedClients>(key.MicroserviceId);
        _connectionId = connectionId;
        await connectedClients.SubscribeDisconnected(this);
        await observer.SetMetadata(name, ObserverType.Client);
        await observer.Subscribe<IClientObserverSubscriber>(eventTypes);
    }

    /// <inheritdoc/>
    public void OnClientDisconnected(ConnectedClient client)
    {
        _clientObserverDisconnectedObservers.Notify(_ => _.OnClientObserverDisconnected(client));
        var id = this.GetPrimaryKey(out var keyAsString);
        var key = ObserverKey.Parse(keyAsString);
        var observer = GrainFactory.GetGrain<IObserver>(id, key);
        observer.Unsubscribe();
    }

    /// <inheritdoc/>
    public Task SubscribeDisconnected(INotifyClientObserverDisconnected subscriber)
    {
        _clientObserverDisconnectedObservers.Subscribe(subscriber, subscriber);
        return Task.CompletedTask;
    }
}
