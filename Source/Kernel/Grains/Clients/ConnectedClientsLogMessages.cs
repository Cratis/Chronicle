// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Grains.Clients;

/// <summary>
/// Holds log messages for <see cref="ConnectedClients"/>.
/// </summary>
internal static partial class ConnectedClientsLogMessages
{
    [LoggerMessage(0, LogLevel.Information, "Connected client with connection identifier '{ConnectionId}'.")]
    internal static partial void ClientConnected(this ILogger<ConnectedClients> logger, string connectionId);

    [LoggerMessage(1, LogLevel.Information, "Disconnected client with connection identifier '{ConnectionId}'. Reason: {Reason}")]
    internal static partial void ClientDisconnected(this ILogger<ConnectedClients> logger, string connectionId, string reason);
}
