// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation.Multiplexed;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Connections.Multiplexed;

#pragma warning disable SA1600 // Elements should be documented
#pragma warning disable MA0048 // File name must match type name

internal static partial class MultiplexedObservationChannelLogMessages
{
    [LoggerMessage(LogLevel.Debug, "Opening multiplexed observation stream {StreamId} as {Kind}")]
    internal static partial void OpeningStream(this ILogger<MultiplexedObservationChannel> logger, ulong streamId, MultiplexedObserverKind kind);

    [LoggerMessage(LogLevel.Debug, "Writing {Count} multiplexed observation messages in one write")]
    internal static partial void WritingMessages(this ILogger<MultiplexedObservationChannel> logger, int count);

    [LoggerMessage(LogLevel.Warning, "Multiplexed observation stream {StreamId} delivered message {Sequence} out of order")]
    internal static partial void OutOfOrderMessage(this ILogger<MultiplexedObservationChannel> logger, ulong streamId, ulong sequence);

    [LoggerMessage(LogLevel.Debug, "Multiplexed observation connection ended with {Streams} streams still open")]
    internal static partial void ConnectionEnded(this ILogger<MultiplexedObservationChannel> logger, int streams, Exception? error);
}
