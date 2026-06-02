// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Observation.States;

#pragma warning disable SA1600 // Elements should be documented
#pragma warning disable MA0048 // File name must match type name
#pragma warning disable SA1402 // File may only contain a single type

internal static partial class CatchingUpInFlightLogMessages
{
    [LoggerMessage(LogLevel.Information, "Recovering {Count} in-flight partition(s) after detecting persisted in-flight event markers")]
    internal static partial void RecoveringInFlightPartitions(this ILogger<CatchingUpInFlight> logger, int count);

    [LoggerMessage(LogLevel.Information, "Starting in-flight catch-up for partition {Partition} from event sequence number {FromSequenceNumber}")]
    internal static partial void StartingInFlightCatchUpForPartition(this ILogger<CatchingUpInFlight> logger, Key partition, EventSequenceNumber fromSequenceNumber);

    [LoggerMessage(LogLevel.Error, "Failed to enqueue in-flight catch-up for one or more partitions; transitioning observer to quarantine")]
    internal static partial void FailedToCatchUpInFlightPartitions(this ILogger<CatchingUpInFlight> logger, Exception exception);
}

internal static class CatchingUpInFlightScopes
{
    internal static IDisposable? BeginCatchingUpInFlightScope(this ILogger<CatchingUpInFlight> logger, ObserverId observerId, ObserverKey observerKey) =>
        logger.BeginScope(new
        {
            ObserverId = observerId,
            observerKey.EventStore,
            observerKey.Namespace,
            observerKey.EventSequenceId
        });
}
