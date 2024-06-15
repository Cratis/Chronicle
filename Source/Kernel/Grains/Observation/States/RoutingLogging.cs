// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Observation;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Grains.Observation.States;

#pragma warning disable SA1600 // Elements should be documented
#pragma warning disable MA0048 // File name must match type name
#pragma warning disable SA1402 // File may only contain a single type

internal static partial class RoutingLogMessages
{
    [LoggerMessage(0, LogLevel.Trace, "Entering routing state")]
    internal static partial void Entering(this ILogger<Routing> logger);

    [LoggerMessage(1, LogLevel.Trace, "Tail sequence numbers for observer is {TailEventSequenceNumber} and {TailEventSequenceNumberForEventTypes}")]
    internal static partial void TailEventSequenceNumbers(this ILogger<Routing> logger, EventSequenceNumber tailEventSequenceNumber, EventSequenceNumber tailEventSequenceNumberForEventTypes);

    [LoggerMessage(2, LogLevel.Trace, "Observer is not subscribed. Transitioning to disconnected state.")]
    internal static partial void NotSubscribed(this ILogger<Routing> logger);

    [LoggerMessage(3, LogLevel.Trace, "Observer is indexing. Transitioning to indexing state.")]
    internal static partial void Indexing(this ILogger<Routing> logger);

    [LoggerMessage(4, LogLevel.Trace, "Observer is catching up. Transitioning to catch up state.")]
    internal static partial void CatchingUp(this ILogger<Routing> logger);

    [LoggerMessage(5, LogLevel.Trace, "Observer is replaying. Transitioning to replay state.")]
    internal static partial void Replaying(this ILogger<Routing> logger);

    [LoggerMessage(6, LogLevel.Trace, "Observer is ready for observing. Transitioning to observing state.")]
    internal static partial void Observing(this ILogger<Routing> logger);

    [LoggerMessage(7, LogLevel.Trace, "Observer will fast forward to tail of event sequence.")]
    internal static partial void FastForwarding(this ILogger<Routing> logger);

    [LoggerMessage(8, LogLevel.Trace, "Observer needs to replay. Transitioning to replay state.")]
    internal static partial void NeedsToReplay(this ILogger<Routing> logger);
}

internal static class RoutingScopes
{
    internal static IDisposable? BeginRoutingScope(this ILogger<Routing> logger, ObserverId observerId, ObserverKey observerKey) =>
        logger.BeginScope(new Dictionary<string, object>
        {
            ["ObserverId"] = observerId,
            ["EventStore"] = observerKey.EventStore,
            ["Namespace"] = observerKey.Namespace,
            ["EventSequenceId"] = observerKey.EventSequenceId
        });
}
