// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Aksio.Cratis.Clients;

internal static partial class WebSocketConnectionLogMessages
{
    [LoggerMessage(0, LogLevel.Information, "Connecting to Kernel over WebSockets @ '{Url}'")]
    internal static partial void Connecting(this ILogger<WebSocketConnection> logger, Uri url);

    [LoggerMessage(1, LogLevel.Information, "Kernel responded with connected")]
    internal static partial void Connected(this ILogger<WebSocketConnection> logger);

    [LoggerMessage(2, LogLevel.Information, "Kernel reconnected")]
    internal static partial void Reconnected(this ILogger<WebSocketConnection> logger);

    [LoggerMessage(3, LogLevel.Information, "Kernel disconnected")]
    internal static partial void Disconnected(this ILogger<WebSocketConnection> logger);

    [LoggerMessage(4, LogLevel.Information, "Send client information (Client version: '{ClientVersion}', MicroserviceId: '{MicroserviceId}', ConnectionId: '{ConnectionId}'")]
    internal static partial void SendingClientInformation(this ILogger<WebSocketConnection> logger, string clientVersion, string microserviceId, string connectionId);
}
