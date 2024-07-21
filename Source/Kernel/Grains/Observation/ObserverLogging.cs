// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Reactions;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Grains.Observation;

#pragma warning disable SA1600 // Elements should be documented
#pragma warning disable MA0048 // File name must match type name
#pragma warning disable SA1402 // File may only contain a single type

internal static partial class ObserverLogMessages
{
    [LoggerMessage(LogLevel.Information, "Subscribing observer")]
    internal static partial void Subscribing(this ILogger<Observer> logger);

    [LoggerMessage(LogLevel.Trace, "Partition {Partition} failed for event with sequence number {EventSequenceNumber}")]
    internal static partial void PartitionFailed(this ILogger<Observer> logger, Key partition, EventSequenceNumber eventSequenceNumber);

    [LoggerMessage(LogLevel.Trace, "Trying to recover partition {Partition}")]
    internal static partial void TryingToRecoverFailedPartition(this ILogger<Observer> logger, Key partition);

    [LoggerMessage(LogLevel.Trace, "Giving up on trying to recover failed partition {Partition} automatically")]
    internal static partial void GivingUpOnRecoveringFailedPartition(this ILogger<Observer> logger, Key partition);
}

internal static class ObserverScopes
{
    internal static IDisposable? BeginObserverScope(this ILogger<Observer> logger, ObserverId observerId, ObserverKey observerKey) =>
        logger.BeginScope(new Dictionary<string, object>
        {
            ["ObserverId"] = observerId,
            ["EventStore"] = observerKey.EventStore,
            ["Namespace"] = observerKey.Namespace,
            ["EventSequenceId"] = observerKey.EventSequenceId
        });
}
