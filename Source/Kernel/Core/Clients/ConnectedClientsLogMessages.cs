// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Clients;

/// <summary>
/// Holds log messages for <see cref="ConnectedClients"/>.
/// </summary>
internal static partial class ConnectedClientsLogMessages
{
    [LoggerMessage(LogLevel.Debug, "Connected client with connection identifier '{ConnectionId}'.")]
    internal static partial void ClientConnected(this ILogger<ConnectedClients> logger, string connectionId);

    [LoggerMessage(LogLevel.Information, "Restored client with connection identifier '{ConnectionId}', which the registry had lost while it stayed connected.")]
    internal static partial void ClientRestored(this ILogger<ConnectedClients> logger, string connectionId);

    [LoggerMessage(LogLevel.Debug, "Disconnected client with connection identifier '{ConnectionId}'. Reason: {Reason}")]
    internal static partial void ClientDisconnected(this ILogger<ConnectedClients> logger, string connectionId, string reason);

    [LoggerMessage(LogLevel.Warning, "Could not tell observer '{ObserverId}' that client '{ConnectionId}' was dropped. The observer watchdog hands it over instead.")]
    internal static partial void FailedToNotifyObserver(this ILogger<ClientDepartureNotifier> logger, string connectionId, string observerId, Exception exception);
}
