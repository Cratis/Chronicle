// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Connections;
using Aksio.Cratis.Events;
using Aksio.Cratis.Kernel.Grains.Clients;
using Aksio.Cratis.Observation;
using Microsoft.Extensions.Logging;

namespace Aksio.Cratis.Kernel.Grains.Observation.Clients;

/// <summary>
/// Represents an implementation of <see cref="IClientObserver"/>.
/// </summary>
public class ClientObserver : Grain, IClientObserver, INotifyClientDisconnected
{
    readonly ILogger<ClientObserver> _logger;
    readonly IExecutionContextManager _executionContextManager;
    ObserverId? _observerId;
    ObserverKey? _observerKey;

    /// <summary>
    /// Initializes a new instance of the <see cref="ClientObserver"/> class.
    /// </summary>
    /// <param name="logger"><see cref="ILogger"/> for logging.</param>
    /// <param name="executionContextManager">The <see cref="IExecutionContextManager"/>.</param>
    public ClientObserver(
        ILogger<ClientObserver> logger,
        IExecutionContextManager executionContextManager)
    {
        _logger = logger;
        _executionContextManager = executionContextManager;
    }

    /// <inheritdoc/>
    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        _observerId = this.GetPrimaryKey(out var keyAsString);
        _observerKey = ObserverKey.Parse(keyAsString);

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task Start(ObserverName name, ConnectionId connectionId, IEnumerable<EventType> eventTypes)
    {
        _executionContextManager.Establish(_observerKey!.TenantId, CorrelationId.New(), _observerKey!.MicroserviceId);
        _logger.Starting(_observerKey!.MicroserviceId, _observerId!, _observerKey!.EventSequenceId, _observerKey!.TenantId);
        var observer = GrainFactory.GetGrain<IObserverSupervisor>(_observerId!, _observerKey!);
        var connectedClients = GrainFactory.GetGrain<IConnectedClients>(_observerKey!.MicroserviceId);
        await connectedClients.SubscribeDisconnected(this.AsReference<INotifyClientDisconnected>());
        await observer.SetNameAndType(name, ObserverType.Client);
        var connectedClient = await connectedClients.GetConnectedClient(connectionId);
        await observer.Subscribe<IClientObserverSubscriber>(eventTypes, connectedClient);
    }

    /// <inheritdoc/>
    public void OnClientDisconnected(ConnectedClient client)
    {
        _logger.ClientDisconnected(client.ConnectionId, _observerKey!.MicroserviceId, _observerId!, _observerKey!.EventSequenceId, _observerKey!.TenantId);
        var id = this.GetPrimaryKey(out var keyAsString);
        var key = ObserverKey.Parse(keyAsString);
        var observer = GrainFactory.GetGrain<IObserverSupervisor>(id, key);
        observer.Unsubscribe();
    }
}
