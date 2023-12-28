// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Connections;
using Aksio.Cratis.Events;
using Aksio.Cratis.Kernel.Grains.Clients;
using Aksio.Cratis.Observation;
using Microsoft.Extensions.Logging;
using Orleans.Placement;
using Orleans.Runtime;

namespace Aksio.Cratis.Kernel.Grains.Observation.Clients;

/// <summary>
/// Represents an implementation of <see cref="IClientObserver"/>.
/// </summary>
[PreferLocalPlacement]
public class ClientObserver : Grain, IClientObserver, INotifyClientDisconnected
{
    readonly ILogger<ClientObserver> _logger;
    readonly IExecutionContextManager _executionContextManager;
    readonly ILocalSiloDetails _localSiloDetails;
    ObserverId? _observerId;
    ConnectedObserverKey? _observerKey;

    /// <summary>
    /// Initializes a new instance of the <see cref="ClientObserver"/> class.
    /// </summary>
    /// <param name="executionContextManager">The <see cref="IExecutionContextManager"/>.</param>
    /// <param name="localSiloDetails"><see cref="ILocalSiloDetails"/> for getting information about the silo this grain is on.</param>
    /// <param name="logger"><see cref="ILogger"/> for logging.</param>
    public ClientObserver(
        IExecutionContextManager executionContextManager,
        ILocalSiloDetails localSiloDetails,
        ILogger<ClientObserver> logger)
    {
        _executionContextManager = executionContextManager;
        _localSiloDetails = localSiloDetails;
        _logger = logger;
    }

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
        _executionContextManager.Establish(_observerKey!.TenantId, CorrelationId.New(), _observerKey!.MicroserviceId);
        _logger.Starting(_observerKey!.MicroserviceId, _observerId!, _observerKey!.EventSequenceId, _observerKey!.TenantId);
        var key = new ObserverKey(_observerKey.MicroserviceId, _observerKey.TenantId, _observerKey.EventSequenceId);
        var observer = GrainFactory.GetGrain<IObserver>(_observerId!, key);
        var connectedClients = GrainFactory.GetGrain<IConnectedClients>(0);
        await connectedClients.SubscribeDisconnected(this.AsReference<INotifyClientDisconnected>());
        var connectedClient = await connectedClients.GetConnectedClient(_observerKey.ConnectionId!);
        await observer.Subscribe<IClientObserverSubscriber>(name, ObserverType.Client, eventTypes, _localSiloDetails.SiloAddress, connectedClient);
    }

    /// <inheritdoc/>
    public void OnClientDisconnected(ConnectedClient client)
    {
        _logger.ClientDisconnected(client.ConnectionId, _observerKey!.MicroserviceId, _observerId!, _observerKey!.EventSequenceId, _observerKey!.TenantId);
        var id = this.GetPrimaryKey(out var _);
        var key = new ObserverKey(_observerKey.MicroserviceId, _observerKey.TenantId, _observerKey.EventSequenceId);
        var observer = GrainFactory.GetGrain<IObserver>(id, key);
        observer.Unsubscribe();
    }

    async Task HandleConnectedClientsSubscription(object state)
    {
        var connectedClients = GrainFactory.GetGrain<IConnectedClients>(0);
        await connectedClients.SubscribeDisconnected(this.AsReference<INotifyClientDisconnected>());
    }
}
