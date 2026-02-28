// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Observation;

#pragma warning disable SA1600 // Elements should be documented
#pragma warning disable MA0048 // File name must match type name
#pragma warning disable SA1402 // File may only contain a single type

internal static partial class ObserverLogMessages
{
    [LoggerMessage(LogLevel.Debug, "Subscribing observer")]
    internal static partial void Subscribing(this ILogger<Observer> logger);

    [LoggerMessage(LogLevel.Trace, "Subscribing observer with {Count} event types: {EventTypes}")]
    internal static partial void SubscribingWithEventTypes(this ILogger<Observer> logger, int count, string eventTypes);

    [LoggerMessage(LogLevel.Warning, "Partition {Partition} failed for event with sequence number {EventSequenceNumber}. Error: {ExceptionMessages}. StackTrace: {StackTrace}")]
    internal static partial void PartitionFailed(this ILogger<Observer> logger, Key partition, EventSequenceNumber eventSequenceNumber, IEnumerable<string> exceptionMessages, string stackTrace);

    [LoggerMessage(LogLevel.Debug, "Trying to recover partition {Partition}")]
    internal static partial void TryingToRecoverFailedPartition(this ILogger<Observer> logger, Key partition);

    [LoggerMessage(LogLevel.Warning, "Giving up on trying to recover failed partition {Partition} automatically")]
    internal static partial void GivingUpOnRecoveringFailedPartition(this ILogger<Observer> logger, Key partition);

    [LoggerMessage(LogLevel.Debug, "Attempting to replay partition {Partition} to event sequence number {ToEventSequenceNumber}")]
    internal static partial void AttemptReplayPartition(this ILogger<Observer> logger, Key partition, EventSequenceNumber toEventSequenceNumber);

    [LoggerMessage(LogLevel.Debug, "Finished replay for partition {Partition}")]
    internal static partial void FinishedReplayForPartition(this ILogger<Observer> logger, Key partition);

    [LoggerMessage(LogLevel.Debug, "Failing partition {Partition} has successfully recovered")]
    internal static partial void FailingPartitionRecovered(this ILogger<Observer> logger, Key partition);

    [LoggerMessage(LogLevel.Debug, "Failing partition {Partition} has partially recovered. Last successfully handled event was {LastEventSequenceNumber}")]
    internal static partial void FailingPartitionPartiallyRecovered(this ILogger<Observer> logger, Key partition, EventSequenceNumber lastEventSequenceNumber);

    [LoggerMessage(LogLevel.Debug, "Partition {Partition} successfully caught up to event sequence number {EventSequenceNumber}")]
    internal static partial void PartitionCaughtUp(this ILogger<Observer> logger, Key partition, EventSequenceNumber eventSequenceNumber);

    [LoggerMessage(LogLevel.Debug, "Resuming catchup for partition {Partition} starting from event sequence number {EventSequenceNumber}")]
    internal static partial void StartingCatchUpForPartition(this ILogger<Observer> logger, Key partition, EventSequenceNumber eventSequenceNumber);

    [LoggerMessage(LogLevel.Debug, "Observer is not subscribed, ignoring handling of event")]
    internal static partial void ObserverIsNotSubscribed(this ILogger<Observer> logger);

    [LoggerMessage(LogLevel.Debug, "Observer is not active, ignoring handling of event")]
    internal static partial void ObserverIsNotActive(this ILogger<Observer> logger);

    [LoggerMessage(LogLevel.Debug, "Partition '{Partition}' is in a failed state, ignoring handling of event")]
    internal static partial void PartitionIsFailed(this ILogger<Observer> logger, Key partition);

    [LoggerMessage(LogLevel.Debug, "Observer is preparing catchup, ignoring handling of event")]
    internal static partial void ObserverIsPreparingCatchup(this ILogger<Observer> logger);

    [LoggerMessage(LogLevel.Debug, "Partition {Partition} is replaying events and cannot accept new events to handle")]
    internal static partial void PartitionReplayingCannotHandleNewEvents(this ILogger<Observer> logger, Key partition);

    [LoggerMessage(LogLevel.Debug, "Partition {Partition} is catching up events and cannot accept new events to handle")]
    internal static partial void PartitionCatchingUpCannotHandleNewEvents(this ILogger<Observer> logger, Key partition);

    [LoggerMessage(LogLevel.Debug, "Partition {Partition} is not starting catch up job because it is a failing partition")]
    internal static partial void PartitionToCatchUpIsFailing(this ILogger<Observer> logger, Key partition);

    [LoggerMessage(LogLevel.Warning, "While evaluating whether Partition {Partition} needs catchup the last handled event was unavailable. This could indicate that the event sequence is in a broken state")]
    internal static partial void LastHandledEventForPartitionUnavailable(this ILogger<Observer> logger, Key partition);

    [LoggerMessage(LogLevel.Debug, "Last handled event reported is not an actual value")]
    internal static partial void LastHandledEventIsNotActualValue(this ILogger<Observer> logger);

    [LoggerMessage(LogLevel.Trace, "Entering catchup state")]
    internal static partial void Entering(this ILogger<Observer> logger);

    [LoggerMessage(LogLevel.Trace, "Existing catch up job running, will let it finish")]
    internal static partial void FinishingExistingCatchUpJob(this ILogger<Observer> logger);

    [LoggerMessage(LogLevel.Trace, "Existing catch up job found - resuming")]
    internal static partial void ResumingCatchUpJob(this ILogger<Observer> logger);

    [LoggerMessage(LogLevel.Trace, "Start new catch up job from event sequence number {EventSequenceNumber}")]
    internal static partial void StartCatchUpJob(this ILogger<Observer> logger, EventSequenceNumber eventSequenceNumber);

    [LoggerMessage(LogLevel.Trace, "Registering partitions that are catching up")]
    internal static partial void RegisteringCatchingUpPartitions(this ILogger<Observer> logger);

    [LoggerMessage(LogLevel.Critical, "Observer failed for unknown reasons after handling events")]
    internal static partial void ObserverFailedForUnknownReasonsAfterHandlingEvents(this ILogger<Observer> logger, Exception exception);

    [LoggerMessage(LogLevel.Debug, "Observer is replaying. Transitioning to replay state.")]
    internal static partial void Replaying(this ILogger<Observer> logger);

    [LoggerMessage(LogLevel.Debug, "Observer needs to replay. Transitioning to replay state.")]
    internal static partial void NeedsToReplay(this ILogger<Observer> logger);
}

internal static class ObserverScopes
{
    internal static IDisposable? BeginObserverScope(this ILogger<Observer> logger, ObserverId observerId, ObserverKey observerKey) =>
        logger.BeginScope(new
        {
            ObserverId = observerId,
            observerKey.EventStore,
            observerKey.Namespace,
            observerKey.EventSequenceId
        });
}
