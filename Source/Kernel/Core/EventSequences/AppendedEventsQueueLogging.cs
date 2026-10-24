// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.EventSequences;

#pragma warning disable SA1600 // Elements should be documented
#pragma warning disable MA0048 // File name must match type name
#pragma warning disable SA1402 // File may only contain a single type

internal static partial class AppendedEventsQueueLogMessages
{
    [LoggerMessage(LogLevel.Error, "Shutdown deadline passed with {PendingItems} unfinished appended-event batches for {EventSequence}")]
    internal static partial void DrainUnfinished(this ILogger<AppendedEventsQueue> logger, EventSequenceKey eventSequence, int pendingItems);

    [LoggerMessage(LogLevel.Error, "Failed notifying observers")]
    internal static partial void NotifyingObserversFailed(this ILogger<AppendedEventsQueue> logger, Exception exception);

    [LoggerMessage(LogLevel.Warning, "Dispatching {Count} event(s) {FirstEventSequenceNumber}-{LastEventSequenceNumber} of partition {Partition} to observer {ObserverKey} failed; they may not have been delivered")]
    internal static partial void SuspectedUndeliveredEvents(this ILogger<AppendedEventsQueue> logger, int count, EventSequenceNumber firstEventSequenceNumber, EventSequenceNumber lastEventSequenceNumber, EventSourceId partition, ObserverKey observerKey);

    [LoggerMessage(LogLevel.Warning, "{Count} further suspected-undelivered warnings were not logged in the last {Seconds} seconds")]
    internal static partial void SuspectedUndeliveredWarningsSuppressed(this ILogger<AppendedEventsQueue> logger, int count, int seconds);

    [LoggerMessage(LogLevel.Error, "An error occurred while handling appended events in queue. Keep on processing.")]
    internal static partial void QueueHandlerFailed(this ILogger<AppendedEventsQueue> logger, Exception exception);

    [LoggerMessage(LogLevel.Debug, "Appended-events queue is full; spilling {NumberOfObservers} observer(s) to catch-up recovery")]
    internal static partial void SpillingToCatchup(this ILogger<AppendedEventsQueue> logger, int numberOfObservers);

    [LoggerMessage(LogLevel.Warning, "Dispatch to observer {ObserverKey} failed for sequence range {FirstEventSequenceNumber}-{LastEventSequenceNumber}; spilling it to catch-up")]
    internal static partial void DispatchFailedSpillingToCatchup(this ILogger<AppendedEventsQueue> logger, ObserverKey observerKey, EventSequenceNumber firstEventSequenceNumber, EventSequenceNumber lastEventSequenceNumber);

    [LoggerMessage(LogLevel.Warning, "Failed to start catch-up for spilled observer {ObserverKey} (attempt {Attempt}); will retry")]
    internal static partial void SpillCatchupTriggerFailed(this ILogger<AppendedEventsQueue> logger, ObserverKey observerKey, int attempt, Exception exception);

    [LoggerMessage(LogLevel.Warning, "Catch-up trigger budget exceeded for spilled observer {ObserverKey}; continuing retries at the capped interval until it starts or the queue is disposed/reset")]
    internal static partial void SpillCatchupTriggerBudgetExceeded(this ILogger<AppendedEventsQueue> logger, ObserverKey observerKey);

    [LoggerMessage(LogLevel.Warning, "Queue was disposed before catch-up could be started for spilled observer {ObserverKey}; it stays behind until it re-subscribes or reactivates")]
    internal static partial void SpillCatchupTriggerAbandonedOnDispose(this ILogger<AppendedEventsQueue> logger, ObserverKey observerKey);

    [LoggerMessage(LogLevel.Debug, "Live backlog for observer {ObserverKey} spans more than {Limit} partition runs; spilling it to catch-up")]
    internal static partial void SpillingFragmentedObserverToCatchup(this ILogger<AppendedEventsQueue> logger, ObserverKey observerKey, int limit);

    [LoggerMessage(LogLevel.Debug, "Observer {ObserverKey} was spilled again while its catch-up was being started; starting catch-up again")]
    internal static partial void CatchupRequestedAgain(this ILogger<AppendedEventsQueue> logger, ObserverKey observerKey);
}
