// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Grains.Clients;
using Cratis.Chronicle.Reactions;
using Cratis.Chronicle.Reactions.Reducers;
using Microsoft.Extensions.Logging;
using Orleans.Runtime;

namespace Cratis.Chronicle.Grains.Observation.Reducers.Clients;

/// <summary>
/// Represents an implementation of <see cref="IClientReducer"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ClientReducer"/>.
/// </remarks>
/// <param name="localSiloDetails"><see cref="ILocalSiloDetails"/> for getting information about the silo this grain is on.</param>
/// <param name="logger"><see cref="ILogger"/> for logging.</param>
public class ClientReducer(
    ILocalSiloDetails localSiloDetails,
    ILogger<ClientReducer> logger) : Grain, IClientReducer, INotifyClientDisconnected
{
    ObserverKey? _observerKey;

    /// <inheritdoc/>
    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        _observerKey = ObserverKey.Parse(this.GetPrimaryKeyString());

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task Start(ObserverName name, ConnectionId connectionId, IEnumerable<EventTypeWithKeyExpression> eventTypes)
    {
        logger.Starting(_observerKey!.EventStore, _observerKey.ObserverId!, _observerKey!.EventSequenceId, _observerKey!.Namespace);
        var observer = GrainFactory.GetGrain<IObserver>(_observerKey.ObserverId!, _observerKey!);
        var connectedClients = GrainFactory.GetGrain<IConnectedClients>(0);

        RegisterTimer(HandleConnectedClientsSubscription, null!, TimeSpan.Zero, TimeSpan.FromSeconds(5));
        var connectedClient = await connectedClients.GetConnectedClient(connectionId);
        await observer.Subscribe<IClientReducerSubscriber>(
            name,
            ObserverType.Reducer,
            eventTypes.Select(_ => _.EventType).ToArray(),
            localSiloDetails.SiloAddress,
            connectedClient);
    }

    /// <inheritdoc/>
    public void OnClientDisconnected(ConnectedClient client)
    {
        logger.ClientDisconnected(client.ConnectionId, _observerKey!.EventStore, _observerKey.ObserverId!, _observerKey!.EventSequenceId, _observerKey!.Namespace);
        var key = ObserverKey.Parse(this.GetPrimaryKeyString());
        var observer = GrainFactory.GetGrain<IObserver>(key);
        observer.Unsubscribe();
    }

    async Task HandleConnectedClientsSubscription(object state)
    {
        var connectedClients = GrainFactory.GetGrain<IConnectedClients>(0);
        await connectedClients.SubscribeDisconnected(this.AsReference<INotifyClientDisconnected>());
    }
}
