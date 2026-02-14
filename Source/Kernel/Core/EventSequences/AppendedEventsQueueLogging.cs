// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.EventSequences;

#pragma warning disable SA1600 // Elements should be documented
#pragma warning disable MA0048 // File name must match type name
#pragma warning disable SA1402 // File may only contain a single type

internal static partial class AppendedEventsQueueLogMessages
{
    [LoggerMessage(LogLevel.Error, "Failed notifying observers")]
    internal static partial void NotifyingObserversFailed(this ILogger<AppendedEventsQueue> logger, Exception exception);

    [LoggerMessage(LogLevel.Error, "An error occurred while handling appended events in queue. Keep on processing.")]
    internal static partial void QueueHandlerFailed(this ILogger<AppendedEventsQueue> logger, Exception exception);
}
