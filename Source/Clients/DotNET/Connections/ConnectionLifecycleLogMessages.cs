// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Aksio.Cratis.Connections;

internal static partial class ConnectionLifecycleLogMessages
{
    [LoggerMessage(1, LogLevel.Information, "Client connected")]
    internal static partial void Connected(this ILogger<ConnectionLifecycle> logger);

    [LoggerMessage(2, LogLevel.Information, "Client disconnected")]
    internal static partial void Disconnected(this ILogger<ConnectionLifecycle> logger);

    [LoggerMessage(3, LogLevel.Error, "Failure during the connected lifecycle event")]
    internal static partial void FailureDuringConnected(this ILogger<ConnectionLifecycle> logger, Exception exception);

    [LoggerMessage(4, LogLevel.Error, "Failure during the disconnected lifecycle event")]
    internal static partial void FailureDuringDisconnected(this ILogger<ConnectionLifecycle> logger, Exception exception);
}
