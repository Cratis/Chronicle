// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Connections;

internal static partial class ConnectionWatchdogLogMessages
{
    [LoggerMessage(LogLevel.Debug, "Reconnecting to Chronicle (attempt {Attempt})")]
    internal static partial void Reconnecting(this ILogger<ConnectionWatchdog> logger, int attempt);

    [LoggerMessage(LogLevel.Error, "Reconnect attempt {Attempt} failed - retrying in {BackoffSeconds} seconds")]
    internal static partial void ReconnectAttemptFailed(this ILogger<ConnectionWatchdog> logger, int attempt, double backoffSeconds, Exception exception);

    [LoggerMessage(LogLevel.Warning, "Notifying about the dropped session failed - continuing with reconnect")]
    internal static partial void SessionDroppedNotificationFailed(this ILogger<ConnectionWatchdog> logger, Exception exception);
}
