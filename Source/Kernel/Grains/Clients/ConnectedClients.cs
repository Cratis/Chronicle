// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Clients;
using Aksio.Cratis.Execution;
using Aksio.Cratis.Kernel.Orleans.Observers;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Providers;

namespace Aksio.Cratis.Kernel.Grains.Clients;

/// <summary>
/// Represents an implementation of <see cref="IConnectedClients"/>.
/// </summary>
[StorageProvider(ProviderName = ConnectedClientsState.StorageProvider)]
public class ConnectedClients : Grain<ConnectedClientsState>, IConnectedClients
{
    /// <summary>
    /// Gets the name of the HTTP client for connected clients.
    /// </summary>
    public const string ConnectedClientsHttpClient = "connected-clients";

    readonly ILogger<ConnectedClients> _logger;
    readonly ObserverManager<INotifyClientDisconnected> _clientDisconnectedObservers;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectedClients"/> class.
    /// </summary>
    /// <param name="logger"><see cref="ILogger"/> for logging.</param>
    public ConnectedClients(ILogger<ConnectedClients> logger)
    {
        _logger = logger;
        _clientDisconnectedObservers = new(TimeSpan.FromMinutes(1), logger, "ClientDisconnectedObservers");
    }

    /// <inheritdoc/>
    public override Task OnActivateAsync()
    {
        RegisterTimer(ReviseConnectedClients, null!, TimeSpan.Zero, TimeSpan.FromSeconds(1));
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task OnClientConnected(ConnectionId connectionId, Uri clientUri, string version)
    {
        var microserviceId = (MicroserviceId)this.GetPrimaryKey();

        _logger.ClientConnected(microserviceId, connectionId);
        State.Clients.Where(_ => _.ClientUri == clientUri).ToList().ForEach(_ => State.Clients.Remove(_));
        State.Clients.Add(new ConnectedClient(connectionId, clientUri, version, DateTimeOffset.UtcNow));

        await WriteStateAsync();
    }

    /// <inheritdoc/>
    public async Task OnClientDisconnected(ConnectionId connectionId, string reason)
    {
        var microserviceId = (MicroserviceId)this.GetPrimaryKey();
        _logger.ClientDisconnected(microserviceId, connectionId, reason);

        var client = State.Clients.FirstOrDefault(_ => _.ConnectionId == connectionId);
        if (client is not null)
        {
            State.Clients.Remove(client);

            await WriteStateAsync();
            _clientDisconnectedObservers.Notify(_ => _.OnClientDisconnected(client));
        }
    }

    /// <inheritdoc/>
    public async Task<bool> OnClientPing(ConnectionId connectionId)
    {
        var client = State.Clients.FirstOrDefault(_ => _.ConnectionId == connectionId);
        if (client is not null)
        {
            State.Clients.Where(_ => _.ClientUri == client.ClientUri).ToList().ForEach(_ => State.Clients.Remove(_));
            State.Clients.Add(client with { LastSeen = DateTimeOffset.UtcNow });
            await WriteStateAsync();
            return true;
        }

        return false;
    }

    /// <inheritdoc/>
    public Task<IEnumerable<ConnectedClient>> GetAllConnectedClients() => Task.FromResult(State.Clients.AsEnumerable());

    /// <inheritdoc/>
    public Task SubscribeDisconnected(INotifyClientDisconnected subscriber)
    {
        _clientDisconnectedObservers.Subscribe(subscriber, subscriber);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task UnsubscribeDisconnected(INotifyClientDisconnected subscriber)
    {
        _clientDisconnectedObservers.Unsubscribe(subscriber);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<bool> IsConnected(ConnectionId connectionId) => Task.FromResult(State.Clients.Any(_ => _.ConnectionId == connectionId));

    /// <inheritdoc/>
    public Task<ConnectedClient> GetConnectedClient(ConnectionId connectionId) => Task.FromResult(State.Clients.First(_ => _.ConnectionId == connectionId));

    async Task ReviseConnectedClients(object state)
    {
        foreach (var connectedClient in State.Clients.ToArray())
        {
            if (connectedClient.LastSeen < DateTimeOffset.UtcNow.AddSeconds(-10))
            {
                await OnClientDisconnected(connectedClient.ConnectionId, "Last seen was more than 2 seconds ago");
            }
        }
    }
}
