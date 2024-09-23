// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Observation.Reactors;
using Cratis.Chronicle.Grains.Clients;
using Microsoft.Extensions.Logging;
using Orleans.Placement;
using Orleans.Providers;

namespace Cratis.Chronicle.Grains.Observation.Reactors.Clients;

/// <summary>
/// Represents an implementation of <see cref="IReactor"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="Reactor"/> class.
/// </remarks>
/// <param name="localSiloDetails"><see cref="ILocalSiloDetails"/> for getting information about the silo this grain is on.</param>
/// <param name="logger"><see cref="ILogger"/> for logging.</param>
[StorageProvider(ProviderName = WellKnownGrainStorageProviders.Reactors)]
[PreferLocalPlacement]
public class Reactor(
    ILocalSiloDetails localSiloDetails,
    ILogger<Reactor> logger) : Grain<ReactorDefinition>, IReactor, INotifyClientDisconnected
{
    IConnectedClients? _connectedClients;
    ConnectedObserverKey? _observerKey;

    /// <inheritdoc/>
    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        _connectedClients = GrainFactory.GetGrain<IConnectedClients>(0);
        await _connectedClients.SubscribeDisconnected(this.AsReference<INotifyClientDisconnected>());

        _observerKey = ConnectedObserverKey.Parse(this.GetPrimaryKeyString());
    }

    /// <inheritdoc/>
    public async Task SetDefinitionAndSubscribe(ReactorDefinition definition)
    {
        logger.Starting(_observerKey!.EventStore, _observerKey.ObserverId!, _observerKey!.EventSequenceId, _observerKey!.Namespace);
        State = definition;
        await WriteStateAsync();

        var key = new ObserverKey(_observerKey.ObserverId, _observerKey.EventStore, _observerKey.Namespace, _observerKey.EventSequenceId);
        var observer = GrainFactory.GetGrain<IObserver>(key);
        await _connectedClients!.SubscribeDisconnected(this.AsReference<INotifyClientDisconnected>());
        var connectedClient = await _connectedClients!.GetConnectedClient(_observerKey.ConnectionId!);
        var eventTypes = definition.EventTypes.Select(e => e.EventType).ToArray();
        await observer.Subscribe<IReactorObserverSubscriber>(ObserverType.Client, eventTypes, localSiloDetails.SiloAddress, connectedClient);
    }

    /// <inheritdoc/>
    public async Task OnClientDisconnected(ConnectedClient client)
    {
        if (client.ConnectionId != _observerKey!.ConnectionId) return;

        logger.ClientDisconnected(client.ConnectionId, _observerKey!.EventStore, _observerKey.ObserverId!, _observerKey!.EventSequenceId, _observerKey!.Namespace);
        var key = new ObserverKey(_observerKey.ObserverId, _observerKey.EventStore, _observerKey.Namespace, _observerKey.EventSequenceId);
        var observer = GrainFactory.GetGrain<IObserver>(key);
        DeactivateOnIdle();
        await Task.WhenAll(
            observer.Unsubscribe(),
            _connectedClients!.UnsubscribeDisconnected(this.AsReference<INotifyClientDisconnected>()));
    }
}
