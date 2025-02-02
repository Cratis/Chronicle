// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Jobs;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Microsoft.Extensions.Logging;
namespace Cratis.Chronicle.Grains.Observation.Jobs;

#pragma warning disable SA1600 // Elements should be documented
#pragma warning disable MA0048 // File name must match type name
#pragma warning disable SA1402 // File may only contain a single type

internal static partial class HandleEventsForPartitionLogging
{
    [LoggerMessage(LogLevel.Debug, "Preparing HandleEventsForPartition job step handling events for partition {Partition} from sequence number {FromSequenceNumber} to sequence number {ToSequenceNumber}")]
    internal static partial void Preparing(this ILogger<HandleEventsForPartition> logger, Key partition, EventSequenceNumber fromSequenceNumber, EventSequenceNumber toSequenceNumber);

    [LoggerMessage(LogLevel.Warning, "Preparing of HandleEventsForPartition job step for partition {Partition} stopped due to the observer subscriber being disconnected")]
    internal static partial void PreparingStoppedUnsubscribed(this ILogger<HandleEventsForPartition> logger, Key partition);

    [LoggerMessage(LogLevel.Debug, "HandleEventsForPartition job step for partition {Partition} successfully prepared")]
    internal static partial void SuccessfullyPrepared(this ILogger<HandleEventsForPartition> logger, Key partition);

    [LoggerMessage(LogLevel.Warning, "Performing HandleEventsForPartition job step for partition {Partition} stopped due to the observer subscriber being disconnected")]
    internal static partial void PerformingStoppedUnsubscribed(this ILogger<HandleEventsForPartition> logger, Key partition);

    [LoggerMessage(LogLevel.Debug, "HandleEventsForPartition job step started handling events for partition {Partition} from sequence number {FromSequenceNumber} to sequence number {ToSequenceNumber}")]
    internal static partial void StartHandling(this ILogger<HandleEventsForPartition> logger, Key partition, EventSequenceNumber fromSequenceNumber, EventSequenceNumber toSequenceNumber);

    [LoggerMessage(LogLevel.Debug, "HandleEventsForPartition job step for partition {Partition} has no more events to handle between sequence number {FromSequenceNumber} and sequence number {ToSequenceNumber}")]
    internal static partial void NoMoreEventsToHandle(this ILogger<HandleEventsForPartition> logger, Key partition, EventSequenceNumber fromSequenceNumber, EventSequenceNumber toSequenceNumber);

    [LoggerMessage(LogLevel.Debug, "HandleEventsForPartition job step for partition {Partition} successfully handled {EventCount} events. Last handled event sequence number is {LastHandledEventSequenceNumber}")]
    internal static partial void SuccessfullyHandledEvents(this ILogger<HandleEventsForPartition> logger, Key partition, EventCount eventCount, EventSequenceNumber lastHandledEventSequenceNumber);

    [LoggerMessage(LogLevel.Warning, "HandleEventsForPartition job step for partition {Partition} successfully handled {EventCount} events, but failed handling {FailedEventSequenceNumber}. Last successfully handled event sequence number is {LastHandledEventSequenceNumber}")]
    internal static partial void FailedHandlingEvents(this ILogger<HandleEventsForPartition> logger, Key partition, EventCount eventCount, EventSequenceNumber failedEventSequenceNumber, EventSequenceNumber lastHandledEventSequenceNumber);

    [LoggerMessage(LogLevel.Warning, "HandleEventsForPartition job step for partition {Partition} failed handling events due to disconnection. Last successfully handled event sequence number is {LastHandledEventSequenceNumber}")]
    internal static partial void EventHandlerDisconnected(this ILogger<HandleEventsForPartition> logger, Key partition, EventSequenceNumber lastHandledEventSequenceNumber);

    [LoggerMessage(LogLevel.Debug, "HandleEventsForPartition job step for partition {Partition} completed successfully all events. Last successfully handled event sequence number is {LastHandledEventSequenceNumber}")]
    internal static partial void HandledAllEvents(this ILogger<HandleEventsForPartition> logger, Key partition, EventSequenceNumber lastHandledEventSequenceNumber);

    [LoggerMessage(LogLevel.Warning, "HandleEventsForPartition job step for partition {Partition} completed handling no events")]
    internal static partial void HandledNoEvents(this ILogger<HandleEventsForPartition> logger, Key partition);

    [LoggerMessage(LogLevel.Warning, "An error occurred for HandleEventsForPartition job step for partition {Partition}")]
    internal static partial void ErrorHandling(this ILogger<HandleEventsForPartition> logger, Exception error, Key partition);

    [LoggerMessage(LogLevel.Warning, "HandleEventsForPartition job step for partition {Partition} was cancelled before handling any events")]
    internal static partial void CancelledBeforeHandlingAnyEvents(this ILogger<HandleEventsForPartition> logger, Key partition);

    [LoggerMessage(LogLevel.Warning, "HandleEventsForPartition job step for partition {Partition} was cancelled and the last handled event sequence number is {LastHandledEventSequenceNumber}")]
    internal static partial void CancelledAfterHandlingEvents(this ILogger<HandleEventsForPartition> logger, Key partition, EventSequenceNumber lastHandledEventSequenceNumber);

    [LoggerMessage(LogLevel.Warning, "HandleEventsForPartition job step failed to persist state about successfully handling event with sequence number {LastHandledEventSequenceNumber}")]
    internal static partial void FailedToPersistSuccessfullyHandledEvent(this ILogger<HandleEventsForPartition> logger, Exception error, EventSequenceNumber lastHandledEventSequenceNumber);

    [LoggerMessage(LogLevel.Warning, "HandleEventsForPartition job step failed, but it had successfully handled some events. Last successfully handled event was: {LastHandledEventSequenceNumber}")]
    internal static partial void FailedWithPartialSuccess(this ILogger<HandleEventsForPartition> logger, Exception error, EventSequenceNumber lastHandledEventSequenceNumber);
}

internal static class HandleEventsForPartitionScopes
{
    internal static IDisposable? BeginObserverScope(this ILogger<HandleEventsForPartition> logger, ObserverKey observerKey, JobId jobId, JobStepId jobStepId) =>
        logger.BeginScope(new
        {
            observerKey.ObserverId,
            observerKey.EventStore,
            observerKey.Namespace,
            observerKey.EventSequenceId,
            JobId = jobId,
            JobStepId = jobStepId
        });
}
