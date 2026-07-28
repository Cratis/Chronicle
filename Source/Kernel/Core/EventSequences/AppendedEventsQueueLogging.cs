// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
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

    [LoggerMessage(LogLevel.Debug, "Appended-events queue is full; spilling {NumberOfObservers} observer(s) to catch-up recovery")]
    internal static partial void SpillingToCatchup(this ILogger<AppendedEventsQueue> logger, int numberOfObservers);

    [LoggerMessage(LogLevel.Warning, "Failed to start catch-up for spilled observer {ObserverKey} (attempt {Attempt} of {MaxAttempts})")]
    internal static partial void SpillCatchupTriggerFailed(this ILogger<AppendedEventsQueue> logger, ObserverKey observerKey, int attempt, int maxAttempts, Exception exception);

    [LoggerMessage(LogLevel.Error, "Gave up starting catch-up for spilled observer {ObserverKey}; it may stay behind until it re-subscribes or reactivates")]
    internal static partial void SpillCatchupTriggerAbandoned(this ILogger<AppendedEventsQueue> logger, ObserverKey observerKey);

    [LoggerMessage(LogLevel.Warning, "Queue was disposed before catch-up could be started for spilled observer {ObserverKey}; it stays behind until it re-subscribes or reactivates")]
    internal static partial void SpillCatchupTriggerAbandonedOnDispose(this ILogger<AppendedEventsQueue> logger, ObserverKey observerKey);
}
