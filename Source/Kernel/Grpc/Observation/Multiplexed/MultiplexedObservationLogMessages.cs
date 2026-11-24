// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation.Multiplexed;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Services.Observation.Multiplexed;

#pragma warning disable SA1600 // Elements should be documented
#pragma warning disable MA0048 // File name must match type name

internal static partial class MultiplexedObservationLogMessages
{
    [LoggerMessage(LogLevel.Debug, "Multiplexed observation connection opened")]
    internal static partial void ConnectionOpened(this ILogger<MultiplexedObservation> logger);

    [LoggerMessage(LogLevel.Debug, "Multiplexed observation connection closed")]
    internal static partial void ConnectionClosed(this ILogger<MultiplexedObservation> logger);

    [LoggerMessage(LogLevel.Debug, "Multiplexed stream {StreamId} opened as {Kind}")]
    internal static partial void StreamOpened(this ILogger<MultiplexedObservation> logger, ulong streamId, MultiplexedObserverKind kind);

    [LoggerMessage(LogLevel.Debug, "Multiplexed stream {StreamId} closed by the client")]
    internal static partial void StreamClosedByClient(this ILogger<MultiplexedObservation> logger, ulong streamId);

    [LoggerMessage(LogLevel.Debug, "Multiplexed stream {StreamId} ended with status {StatusCode}")]
    internal static partial void StreamEnded(this ILogger<MultiplexedObservation> logger, ulong streamId, int statusCode);

    [LoggerMessage(LogLevel.Warning, "Ignoring a message for multiplexed stream {StreamId}: it does not open a stream ({Kind}) and no such stream is open")]
    internal static partial void UnknownStreamMessage(this ILogger<MultiplexedObservation> logger, ulong streamId, MultiplexedObserverKind kind);
}
