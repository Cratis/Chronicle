// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Aksio.Cratis.Kernel.Grains.Clients;

/// <summary>
/// Holds log messages for <see cref="ConnectedClients"/>.
/// </summary>
public static partial class ConnectedClientsLogMessages
{
    [LoggerMessage(0, LogLevel.Information, "Connected client for microservice '{MicroserviceId}' with connection identifier '{ConnectionId}'.")]
    internal static partial void ClientConnected(this ILogger<ConnectedClients> logger, string microserviceId, string connectionId);

    [LoggerMessage(1, LogLevel.Information, "Disconnected client for microservice '{MicroserviceId}' with connection identifier '{ConnectionId}'.")]
    internal static partial void ClientDisconnected(this ILogger<ConnectedClients> logger, string microserviceId, string connectionId);
}
