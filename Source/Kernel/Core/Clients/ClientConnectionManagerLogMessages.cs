// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Clients;

#pragma warning disable SA1600 // Elements should be documented
#pragma warning disable MA0048 // File name must match type name
#pragma warning disable SA1402 // File may only contain a single type

internal static partial class ClientConnectionManagerLogMessages
{
    [LoggerMessage(LogLevel.Debug, "Registered connection {ConnectionId}")]
    internal static partial void ConnectionRegistered(this ILogger<ClientConnectionManager> logger, string connectionId);

    [LoggerMessage(LogLevel.Debug, "Unregistered connection {ConnectionId}")]
    internal static partial void ConnectionUnregistered(this ILogger<ClientConnectionManager> logger, string connectionId);

    [LoggerMessage(LogLevel.Information, "Disconnecting all clients ({Count} connections). Reason: {Reason}")]
    internal static partial void DisconnectingAllClients(this ILogger<ClientConnectionManager> logger, string reason, int count);

    [LoggerMessage(LogLevel.Information, "Blocking new client connections")]
    internal static partial void BlockingNewConnections(this ILogger<ClientConnectionManager> logger);

    [LoggerMessage(LogLevel.Information, "Allowing new client connections")]
    internal static partial void AllowingNewConnections(this ILogger<ClientConnectionManager> logger);
}
