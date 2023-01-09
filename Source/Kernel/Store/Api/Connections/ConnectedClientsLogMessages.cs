// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Aksio.Cratis.Events.Store.Api;

public static partial class ConnectedClientsLogMessages
{
    [LoggerMessage(0, LogLevel.Information, "Client connected")]
    internal static partial void ClientConnected(this ILogger<ConnectedClients> logger);

    [LoggerMessage(1, LogLevel.Information, "Client disconnected")]
    internal static partial void ClientDisconnected(this ILogger<ConnectedClients> logger);
}
