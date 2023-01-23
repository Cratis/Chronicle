// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
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
    readonly IHttpClientFactory _httpClientFactory;
    readonly ILogger<ConnectedClients> _logger;
    readonly ObserverManager<INotifyClientDisconnected> _clientDisconnectedObservers;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectedClients"/> class.
    /// </summary>
    /// <param name="httpClientFactory"><see cref="IHttpClientFactory"/> for connections..</param>
    /// <param name="logger"><see cref="ILogger"/> for logging.</param>
    public ConnectedClients(IHttpClientFactory httpClientFactory, ILogger<ConnectedClients> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _clientDisconnectedObservers = new(TimeSpan.FromMinutes(1), logger, "ClientDisconnectedObservers");
    }

    /// <inheritdoc/>
    public override Task OnActivateAsync()
    {
        RegisterTimer(PingClients, null!, TimeSpan.Zero, TimeSpan.FromSeconds(1));
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task OnClientConnected(ConnectionId connectionId, Uri clientUri, string version)
    {
        var microserviceId = (MicroserviceId)this.GetPrimaryKey();

        _logger.ClientConnected(microserviceId, connectionId);
        State.Clients.Add(new ConnectedClient(connectionId, clientUri, version, DateTimeOffset.UtcNow));

        await WriteStateAsync();
    }

    /// <inheritdoc/>
    public async Task OnClientDisconnected(ConnectionId connectionId)
    {
        var microserviceId = (MicroserviceId)this.GetPrimaryKey();
        _logger.ClientDisconnected(microserviceId, connectionId);

        var client = State.Clients.FirstOrDefault(_ => _.ConnectionId == connectionId);
        if (client is not null)
        {
            State.Clients.Remove(client);

            await WriteStateAsync();
            _clientDisconnectedObservers.Notify(_ => _.OnClientDisconnected(client));
        }
    }

    /// <inheritdoc/>
    public async Task OnClientPing(ConnectionId connectionId)
    {
        var client = State.Clients.FirstOrDefault(_ => _.ConnectionId == connectionId);
        if (client is not null)
        {
            State.Clients.Remove(client);
            State.Clients.Add(client with { LastSeen = DateTimeOffset.UtcNow });
        }

        await WriteStateAsync();
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

    async Task PingClients(object state)
    {
        foreach (var connectedClient in State.Clients.ToArray())
        {
            using var client = _httpClientFactory.CreateClient();
            client.BaseAddress = connectedClient.ClientUri;
            try
            {
                var response = await client.GetAsync("/.cratis/client/ping");
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    await OnClientDisconnected(connectedClient.ConnectionId);
                }
            }
            catch
            {
                await OnClientDisconnected(connectedClient.ConnectionId);
            }
        }
    }
}
