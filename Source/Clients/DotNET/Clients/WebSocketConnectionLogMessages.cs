// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Aksio.Cratis.Clients;

public static partial class WebSocketConnectionLogMessages
{
    [LoggerMessage(0, LogLevel.Information, "Connecting to Kernel over WebSockets @ '{Url}'")]
    public static partial void Connecting(this ILogger<WebSocketConnection> logger, Uri url);
}
