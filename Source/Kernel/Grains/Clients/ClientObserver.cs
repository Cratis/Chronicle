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
    readonly ILogger<ClientObserver> _logger;
    ObserverId? _observerId;
    ObserverKey? _observerKey;

    public ClientObserver(ILogger<ClientObserver> logger)
    {
        _clientObserverDisconnectedObservers = new(TimeSpan.FromMinutes(1), logger, "ClientObserverDisconnectedObservers");
        _logger = logger;
    }

    /// <inheritdoc/>
    public override Task OnActivateAsync()
    {
        _observerId = this.GetPrimaryKey(out var keyAsString);
        _observerKey = ObserverKey.Parse(keyAsString);

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task Start(ObserverName name, ConnectionId connectionId, IEnumerable<EventType> eventTypes)
    {
        _logger.Starting(_observerKey!.MicroserviceId, _observerId!, _observerKey!.EventSequenceId, _observerKey!.TenantId);
        var observer = GrainFactory.GetGrain<IObserver>(_observerId!, _observerKey!);
        var connectedClients = GrainFactory.GetGrain<IConnectedClients>(_observerKey!.MicroserviceId);
        await connectedClients.SubscribeDisconnected(this);
        await observer.SetMetadata(name, ObserverType.Client);
        await observer.Subscribe<IClientObserverSubscriber>(eventTypes);
    }

    /// <inheritdoc/>
    public void OnClientDisconnected(ConnectedClient client)
    {
        _logger.ClientDisconnected(client.ConnectionId, _observerKey!.MicroserviceId, _observerId!, _observerKey!.EventSequenceId, _observerKey!.TenantId);
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
