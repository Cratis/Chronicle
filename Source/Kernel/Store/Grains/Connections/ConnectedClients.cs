// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Execution;
using Microsoft.Extensions.Logging;
using Orleans;

namespace Aksio.Cratis.Events.Store.Grains.Connections;

/// <summary>
/// Represents an implementation of <see cref="IConnectedClients"/>.
/// </summary>
public class ConnectedClients : Grain, IConnectedClients
{
    readonly Dictionary<MicroserviceId, IList<ClientInformation>> _connectionsPerMicroservice = new();
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
    public Task OnClientConnected(ClientInformation clientInformation)
    {
        _logger.ClientDisconnected(clientInformation.MicroserviceId, clientInformation.ConnectionId);

        if (!_connectionsPerMicroservice.ContainsKey(clientInformation.MicroserviceId))
        {
            _connectionsPerMicroservice[clientInformation.MicroserviceId] = new List<ClientInformation>();
        }
        _connectionsPerMicroservice[clientInformation.MicroserviceId].Add(clientInformation);

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task OnClientDisconnected(
        MicroserviceId microserviceId,
        ConnectionId connectionId)
    {
        _logger.ClientDisconnected(microserviceId, connectionId);

        if (_connectionsPerMicroservice.ContainsKey(microserviceId))
        {
            var clientConnection = _connectionsPerMicroservice[microserviceId].FirstOrDefault(_ => _.ConnectionId == connectionId);
            if (clientConnection is not null)
            {
                _connectionsPerMicroservice[microserviceId].Remove(clientConnection);
            }
        }

        return Task.CompletedTask;
    }
}
