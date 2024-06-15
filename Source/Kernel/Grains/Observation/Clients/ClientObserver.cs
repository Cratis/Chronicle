// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Grains.Clients;
using Cratis.Chronicle.Observation;
using Microsoft.Extensions.Logging;
using Orleans.Placement;
using Orleans.Runtime;

namespace Cratis.Chronicle.Grains.Observation.Clients;

/// <summary>
/// Represents an implementation of <see cref="IClientObserver"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ClientObserver"/> class.
/// </remarks>
/// <param name="localSiloDetails"><see cref="ILocalSiloDetails"/> for getting information about the silo this grain is on.</param>
/// <param name="logger"><see cref="ILogger"/> for logging.</param>
[PreferLocalPlacement]
public class ClientObserver(
    ILocalSiloDetails localSiloDetails,
    ILogger<ClientObserver> logger) : Grain, IClientObserver, INotifyClientDisconnected
{
    ObserverId? _observerId;
    ConnectedObserverKey? _observerKey;

    /// <inheritdoc/>
    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        _observerId = this.GetPrimaryKey(out var keyAsString);
        _observerKey = ConnectedObserverKey.Parse(keyAsString);

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task Start(ObserverName name, IEnumerable<EventType> eventTypes)
    {
        logger.Starting(_observerKey!.EventStore, _observerId!, _observerKey!.EventSequenceId, _observerKey!.Namespace);
        var key = new ObserverKey(_observerKey.EventStore, _observerKey.Namespace, _observerKey.EventSequenceId);
        var observer = GrainFactory.GetGrain<IObserver>(_observerId!, key);
        var connectedClients = GrainFactory.GetGrain<IConnectedClients>(0);
        await connectedClients.SubscribeDisconnected(this.AsReference<INotifyClientDisconnected>());
        var connectedClient = await connectedClients.GetConnectedClient(_observerKey.ConnectionId!);
        await observer.Subscribe<IClientObserverSubscriber>(name, ObserverType.Client, eventTypes, localSiloDetails.SiloAddress, connectedClient);
    }

    /// <inheritdoc/>
    public void OnClientDisconnected(ConnectedClient client)
    {
        logger.ClientDisconnected(client.ConnectionId, _observerKey!.EventStore, _observerId!, _observerKey!.EventSequenceId, _observerKey!.Namespace);
        var id = this.GetPrimaryKey(out var _);
        var key = new ObserverKey(_observerKey.EventStore, _observerKey.Namespace, _observerKey.EventSequenceId);
        var observer = GrainFactory.GetGrain<IObserver>(id, key);
        observer.Unsubscribe();
    }
}
