// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Aksio.Cratis.Events.Store.Grains.Connections;

/// <summary>
/// Holds log messages for <see cref="ConnectedClients"/>.
/// </summary>
public static partial class ConnectedClientsLogMessages
{
    [LoggerMessage(0, LogLevel.Information, "Connected client with connection identifier '{ConnectionId}'.")]
    internal static partial void ClientConnected(this ILogger logger, string connectionId);

    [LoggerMessage(1, LogLevel.Information, "Disconnected client with connection identifier '{ConnectionId}'.")]
    internal static partial void ClientDisconnected(this ILogger logger, string connectionId);
}
