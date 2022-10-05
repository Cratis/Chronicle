// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Configuration;
using Aksio.Cratis.Events.Store.Grains.Connections;
using Aksio.Cratis.Events.Store.Observation;
using Aksio.Cratis.Execution;
using Aksio.Cratis.Extensions.Orleans.Execution;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Providers;

namespace Aksio.Cratis.Events.Store.Grains.Observation;

/// <summary>
/// Represents an implementation of <see cref="IClientObservers"/>.
/// </summary>
[StorageProvider(ProviderName = ClientObserversState.StorageProvider)]
public class ClientObservers : Grain<ClientObserversState>, IClientObservers
{
    readonly IExecutionContextManager _executionContextManager;
    readonly IRequestContextManager _requestContextManager;
    readonly Tenants _tenants;
    readonly ILogger<ClientObservers> _logger;
    IConnectedClients? _connectedClients;

    /// <summary>
    /// Initializes a new instance of the <see cref="ClientObservers"/> class.
    /// </summary>
    /// <param name="executionContextManager"><see cref="IExecutionContextManager"/> for establishing execution context.</param>
    /// <param name="requestContextManager"><see cref="IRequestContextManager"/> for working with the Orleans request context.</param>
    /// <param name="tenants">All configured <see cref="Tenants"/>.</param>
    /// <param name="logger">Logger for logging.</param>
    public ClientObservers(
        IExecutionContextManager executionContextManager,
        IRequestContextManager requestContextManager,
        Tenants tenants,
        ILogger<ClientObservers> logger)
    {
        _executionContextManager = executionContextManager;
        _requestContextManager = requestContextManager;
        _tenants = tenants;
        _logger = logger;
    }

    /// <inheritdoc/>
    public override async Task OnActivateAsync()
    {
        _connectedClients = GrainFactory.GetGrain<IConnectedClients>(Guid.Empty);

        // TODO: Subscribe any observers from state, but only if the client is actually still connected.
        await base.OnActivateAsync();
    }

    /// <inheritdoc/>
    public async Task Subscribe(ObserverName name, ObserverId observerId, EventSequenceId eventSequenceId, IEnumerable<EventType> eventTypes)
    {
        var connectionId = _requestContextManager.Get(RequestContextKeys.ConnectionId).ToString()!;
        if (!State.HasConnectionId(connectionId))
        {
            await _connectedClients!.SubscribeOnDisconnected(connectionId, this.GetReference<IConnectedClientObserver>());
        }

        var microserviceId = _executionContextManager.Current.MicroserviceId;
        foreach (var tenantId in _tenants.GetTenantIds())
        {
            var observerKey = new ObserverKey(microserviceId, tenantId, eventSequenceId);
            State.AssociateObserverWithConnectionId(connectionId, new(observerId, observerKey));

            var observer = GrainFactory.GetGrain<IObserver>(observerId, observerKey);
            await observer.SetMetadata(name, ObserverType.Client);
            await observer.Subscribe(eventTypes, connectionId);
        }

        await WriteStateAsync();
    }

    /// <inheritdoc/>
    public void Disconnected(string connectionId)
    {
        _logger.Disconnected(connectionId);
        if (!State.HasConnectionId(connectionId))
        {
            return;
        }

        foreach (var observerIdentifier in State.GetObserversForConnectionId(connectionId))
        {
            var observer = GrainFactory.GetGrain<IObserver>(observerIdentifier.ObserverId, observerIdentifier.ObserverKey);
            observer.Unsubscribe();
        }

        State.Disconnected(connectionId);
        WriteStateAsync();
    }
}
