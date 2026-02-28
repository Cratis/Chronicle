// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Connections;

internal static partial class ConnectionLifecycleLogMessages
{
    [LoggerMessage(LogLevel.Debug, "Client connected")]
    internal static partial void Connected(this ILogger<ConnectionLifecycle> logger);

    [LoggerMessage(LogLevel.Debug, "Client disconnected")]
    internal static partial void Disconnected(this ILogger<ConnectionLifecycle> logger);

    [LoggerMessage(LogLevel.Error, "Failure during the connected lifecycle event")]
    internal static partial void FailureDuringConnected(this ILogger<ConnectionLifecycle> logger, Exception exception);

    [LoggerMessage(LogLevel.Error, "Failure during the disconnected lifecycle event")]
    internal static partial void FailureDuringDisconnected(this ILogger<ConnectionLifecycle> logger, Exception exception);
}
