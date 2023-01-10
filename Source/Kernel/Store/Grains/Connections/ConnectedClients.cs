// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events.Store.Connections;
using Aksio.Cratis.Execution;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Providers;

namespace Aksio.Cratis.Events.Store.Grains.Connections;

/// <summary>
/// Represents an implementation of <see cref="IConnectedClients"/>.
/// </summary>
[StorageProvider(ProviderName = ConnectedClientsState.StorageProvider)]
public class ConnectedClients : Grain<ConnectedClientsState>, IConnectedClients
{
    readonly ILogger<ConnectedClients> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectedClients"/> class.
    /// </summary>
    /// <param name="logger"><see cref="ILogger"/> for logging.</param>
    public ConnectedClients(ILogger<ConnectedClients> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc/>
    public override Task OnActivateAsync() => base.OnActivateAsync();

    /// <inheritdoc/>
    public async Task OnClientConnected(ConnectionId connectionId, Uri clientUri, string version)
    {
        var microserviceId = (MicroserviceId)this.GetPrimaryKey();

        _logger.ClientConnected(microserviceId, connectionId);
        State.Clients.Add(new ConnectedClient(connectionId, clientUri.ToString(), version, DateTimeOffset.UtcNow));

        await WriteStateAsync();
    }

    /// <inheritdoc/>
    public async Task OnClientDisconnected(ConnectionId connectionId)
    {
        var microserviceId = (MicroserviceId)this.GetPrimaryKey();
        _logger.ClientDisconnected(microserviceId, connectionId);

        var client = State.Clients.Find(_ => _.ConnectionId == connectionId);
        if (client is not null)
        {
            State.Clients.Remove(client);
        }

        await WriteStateAsync();
    }

    /// <inheritdoc/>
    public async Task OnClientPing(ConnectionId connectionId)
    {
        var client = State.Clients.Find(_ => _.ConnectionId == connectionId);
        if (client is not null)
        {
            State.Clients.Remove(client);
            State.Clients.Add(client with { LastSeen = DateTimeOffset.UtcNow });
        }

        await WriteStateAsync();
    }
}
