// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Clients;
using Aksio.Cratis.Events;
using Aksio.Cratis.EventSequences;
using Aksio.Cratis.Execution;
using Aksio.Cratis.Observation;
using Microsoft.Extensions.Logging;
using Orleans;

namespace Aksio.Cratis.Kernel.Grains.Clients;

/// <summary>
/// Represents an implementation of <see cref="IClientObserverSubscriber"/>.
/// </summary>
public class ClientObserverSubscriber : Grain, IClientObserverSubscriber, INotifyClientObserverDisconnected
{
    readonly List<ConnectedClient> _clients = new();
    readonly ILogger<ClientObserverSubscriber> _logger;
    MicroserviceId _microserviceId = MicroserviceId.Unspecified;
    ObserverId _observerId = ObserverId.Unspecified;
    TenantId _tenantId = TenantId.NotSet;
    EventSequenceId _eventSequenceId = EventSequenceId.Unspecified;

    public ClientObserverSubscriber(ILogger<ClientObserverSubscriber> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc/>
    public override Task OnActivateAsync()
    {
        var id = this.GetPrimaryKey(out var keyAsString);
        var key = ObserverSubscriberKey.Parse(keyAsString);
        _microserviceId = key.MicroserviceId;
        _observerId = id;
        _tenantId = key.TenantId;
        _eventSequenceId = key.EventSequenceId;

        var clientObserver = GrainFactory.GetGrain<IClientObserver>(id, new ObserverKey(
            key.MicroserviceId,
            key.TenantId,
            key.EventSequenceId,
            key.SourceMicroserviceId,
            key.SourceTenantId));

        clientObserver.SubscribeDisconnected(this);

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public void OnClientObserverDisconnected(ConnectedClient client)
    {
        var actualClient = _clients.Find(_ => _ == client);
        if (actualClient is not null)
        {
            _clients.Remove(actualClient);
        }
    }

    /// <inheritdoc/>
    public async Task OnNext(AppendedEvent @event)
    {
        if (_clients.Count == 0 && _microserviceId != MicroserviceId.Unspecified)
        {
            var connectedClients = GrainFactory.GetGrain<IConnectedClients>(_microserviceId);
            _clients.AddRange(await connectedClients.GetAllConnectedClients());
        }

        _logger.EventReceived(
            _observerId,
            _microserviceId,
            _tenantId,
            @event.Metadata.Type.Id,
            _eventSequenceId,
            @event.Context.SequenceNumber);

        // Call client
    }
}
