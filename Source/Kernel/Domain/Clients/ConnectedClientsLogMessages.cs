// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Aksio.Cratis.Kernel.Domain.Clients;

internal static partial class ConnectedClientsLogMessages
{
    [LoggerMessage(0, LogLevel.Information, "Client (v{Version}) for microservice '{MicroserviceId}' connected with connection identifier '{ConnectionId}'")]
    internal static partial void ClientConnected(this ILogger<ConnectedClients> logger, string version, string microserviceId, string connectionId);

    [LoggerMessage(1, LogLevel.Information, "Client for microservice '{MicroserviceId}' disconnected with connection identifier '{ConnectionId}'")]
    internal static partial void ClientDisconnected(this ILogger<ConnectedClients> logger, string microserviceId, string connectionId);
}
