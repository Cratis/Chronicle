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

    [LoggerMessage(LogLevel.Debug, "HandleEventsForPartition job step for partition {Partition} completed successfully. Last successfully handled event sequence number is {LastHandledEventSequenceNumber}")]
    internal static partial void Completed(this ILogger<HandleEventsForPartition> logger, Key partition, EventSequenceNumber lastHandledEventSequenceNumber);
    
    [LoggerMessage(LogLevel.Warning, "An error occurred for HandleEventsForPartition job step for partition {Partition}")]
    internal static partial void ErrorHandling(this ILogger<HandleEventsForPartition> logger, Exception error, Key partition);
}

internal static class HandleEventsForPartitionScopes
{
    internal static IDisposable? BeginObserverScope(this ILogger<HandleEventsForPartition> logger, ObserverKey observerKey, JobId jobId, JobStepId jobStepId) =>
        logger.BeginScope(new Dictionary<string, object>
        {
            ["ObserverId"] = observerKey.ObserverId,
            ["EventStore"] = observerKey.EventStore,
            ["Namespace"] = observerKey.Namespace,
            ["EventSequenceId"] = observerKey.EventSequenceId,
            ["JobId"] = jobId,
            ["JobStepId"] = jobStepId
        });
}
