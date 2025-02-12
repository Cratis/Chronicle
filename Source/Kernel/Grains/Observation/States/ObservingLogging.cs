// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Storage.Observation;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Grains.Observation.States;

#pragma warning disable SA1600 // Elements should be documented
#pragma warning disable MA0048 // File name must match type name
#pragma warning disable SA1402 // File may only contain a single type

internal static partial class ObservingLogMessages
{
    [LoggerMessage(LogLevel.Trace, "Entering observing state")]
    internal static partial void Entering(this ILogger<Observing> logger);

    [LoggerMessage(LogLevel.Trace, "Subscribing to stream from event sequence number {EventSequenceNumber}")]
    internal static partial void SubscribingToStream(this ILogger<Observing> logger, EventSequenceNumber eventSequenceNumber);
}

internal static class ObservingScopes
{
    internal static IDisposable? BeginObservingScope(
        this ILogger<Observing> logger,
        ObserverState state,
        EventStoreName eventStore,
        EventStoreNamespaceName @namespace,
        EventSequenceId eventSequenceId) =>
        logger.BeginScope(new
        {
            ObserverId = state.Id,
            EventStore = eventStore,
            Namespace = @namespace,
            EventSequenceId = eventSequenceId
        });
}
