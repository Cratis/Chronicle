// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Observation;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Grains.Observation.States;

#pragma warning disable SA1600 // Elements should be documented
#pragma warning disable MA0048 // File name must match type name
#pragma warning disable SA1402 // File may only contain a single type

internal static partial class CatchUpLogMessages
{
    [LoggerMessage(LogLevel.Trace, "Entering catchup state")]
    internal static partial void Entering(this ILogger<CatchUp> logger);

    [LoggerMessage(LogLevel.Trace, "Existing catch up job running, will let it finish")]
    internal static partial void FinishingExistingCatchUpJob(this ILogger<CatchUp> logger);

    [LoggerMessage(LogLevel.Trace, "Existing catch up job found - resuming")]
    internal static partial void ResumingCatchUpJob(this ILogger<CatchUp> logger);

    [LoggerMessage(LogLevel.Trace, "Start new catch up job from event sequence number {EventSequenceNumber}")]
    internal static partial void StartCatchUpJob(this ILogger<CatchUp> logger, EventSequenceNumber eventSequenceNumber);
}

internal static class CatchUpScopes
{
    internal static IDisposable? BeginCatchUpScope(this ILogger<CatchUp> logger, ObserverId observerId, ObserverKey observerKey) =>
        logger.BeginScope(new Dictionary<string, object>
        {
            ["ObserverId"] = observerId,
            ["EventStore"] = observerKey.EventStore,
            ["Namespace"] = observerKey.Namespace,
            ["EventSequenceId"] = observerKey.EventSequenceId
        });
}
