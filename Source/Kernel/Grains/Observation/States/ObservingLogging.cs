// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Events;
using Cratis.EventSequences;
using Cratis.Kernel.Storage.Observation;
using Microsoft.Extensions.Logging;

namespace Cratis.Kernel.Grains.Observation.States;

#pragma warning disable SA1600 // Elements should be documented
#pragma warning disable MA0048 // File name must match type name
#pragma warning disable SA1402 // File may only contain a single type

internal static partial class ObservingLogMessages
{
    [LoggerMessage(0, LogLevel.Trace, "Entering observing state")]
    internal static partial void Entering(this ILogger<Observing> logger);

    [LoggerMessage(1, LogLevel.Trace, "Subscribing to stream from event sequence number {EventSequenceNumber}")]
    internal static partial void SubscribingToStream(this ILogger<Observing> logger, EventSequenceNumber eventSequenceNumber);
}

internal static class ObservingScopes
{
    internal static IDisposable? BeginObservingScope(
        this ILogger<Observing> logger,
        ObserverState state,
        MicroserviceId microserviceId,
        TenantId tenantId,
        EventSequenceId eventSequenceId) =>
        logger.BeginScope(new Dictionary<string, object>
        {
            ["ObserverId"] = state.ObserverId,
            ["MicroserviceId"] = microserviceId,
            ["TenantId"] = tenantId,
            ["EventSequenceId"] = eventSequenceId
        });
}
