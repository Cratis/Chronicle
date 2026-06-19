// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Observation.Jobs;

#pragma warning disable SA1600 // Elements should be documented
#pragma warning disable MA0048 // File name must match type name
#pragma warning disable SA1402 // File may only contain a single type

internal static partial class HandleEventsForObserverLogging
{
    [LoggerMessage(LogLevel.Debug, "Preparing HandleEventsForObserver job step handling events from sequence number {FromSequenceNumber} to sequence number {ToSequenceNumber}")]
    internal static partial void Preparing(this ILogger<HandleEventsForObserver> logger, EventSequenceNumber fromSequenceNumber, EventSequenceNumber toSequenceNumber);

    [LoggerMessage(LogLevel.Warning, "Preparing of HandleEventsForObserver job step stopped due to the observer subscriber being disconnected")]
    internal static partial void PreparingStoppedUnsubscribed(this ILogger<HandleEventsForObserver> logger);

    [LoggerMessage(LogLevel.Debug, "HandleEventsForObserver job step successfully prepared")]
    internal static partial void SuccessfullyPrepared(this ILogger<HandleEventsForObserver> logger);

    [LoggerMessage(LogLevel.Warning, "Performing HandleEventsForObserver job step stopped due to the observer subscriber being disconnected")]
    internal static partial void PerformingStoppedUnsubscribed(this ILogger<HandleEventsForObserver> logger);

    [LoggerMessage(LogLevel.Warning, "HandleEventsForObserver job step successfully handled {EventCount} events for partition {Partition}, but failed handling event at event sequence number {FailedEventSequenceNumber}. Last successfully handled event sequence number is {LastHandledEventSequenceNumber}")]
    internal static partial void FailedHandlingEvents(this ILogger<HandleEventsForObserver> logger, Key partition, EventCount eventCount, EventSequenceNumber failedEventSequenceNumber, EventSequenceNumber lastHandledEventSequenceNumber);

    [LoggerMessage(LogLevel.Warning, "HandleEventsForObserver job step failed handling events for partition {Partition} due to disconnection. Last successfully handled event sequence number is {LastHandledEventSequenceNumber}")]
    internal static partial void EventHandlerDisconnected(this ILogger<HandleEventsForObserver> logger, Key partition, EventSequenceNumber lastHandledEventSequenceNumber);

    [LoggerMessage(LogLevel.Debug, "HandleEventsForObserver job step completed successfully all events. Last successfully handled event sequence number is {LastHandledEventSequenceNumber}")]
    internal static partial void HandledAllEvents(this ILogger<HandleEventsForObserver> logger, EventSequenceNumber lastHandledEventSequenceNumber);

    [LoggerMessage(LogLevel.Warning, "HandleEventsForObserver job step completed handling no events")]
    internal static partial void HandledNoEvents(this ILogger<HandleEventsForObserver> logger);

    [LoggerMessage(LogLevel.Warning, "An error occurred for HandleEventsForObserver job step for partition {Partition}")]
    internal static partial void ErrorHandling(this ILogger<HandleEventsForObserver> logger, Exception error, Key partition);

    [LoggerMessage(LogLevel.Warning, "HandleEventsForObserver job step was cancelled before handling any events")]
    internal static partial void CancelledBeforeHandlingAnyEvents(this ILogger<HandleEventsForObserver> logger);

    [LoggerMessage(LogLevel.Warning, "HandleEventsForObserver job step was cancelled and the last handled event sequence number is {LastHandledEventSequenceNumber}")]
    internal static partial void CancelledAfterHandlingEvents(this ILogger<HandleEventsForObserver> logger, EventSequenceNumber lastHandledEventSequenceNumber);

    [LoggerMessage(LogLevel.Warning, "HandleEventsForObserver job step failed to persist state about successfully handling event with sequence number {LastHandledEventSequenceNumber}")]
    internal static partial void FailedToPersistSuccessfullyHandledEvent(this ILogger<HandleEventsForObserver> logger, Exception error, EventSequenceNumber lastHandledEventSequenceNumber);

    [LoggerMessage(LogLevel.Warning, "HandleEventsForObserver job step failed, but it had successfully handled some events. Last successfully handled event was: {LastHandledEventSequenceNumber}")]
    internal static partial void FailedWithPartialSuccess(this ILogger<HandleEventsForObserver> logger, Exception error, EventSequenceNumber lastHandledEventSequenceNumber);
}

