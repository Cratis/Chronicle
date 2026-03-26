// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences;

/// <summary>
/// Logging extensions for <see cref="EventSequenceStorage"/>.
/// </summary>
internal static partial class EventSequenceStorageLogging
{
    [LoggerMessage(LogLevel.Error, "Failed to append event with sequence number {SequenceNumber} to event sequence {EventSequenceId}")]
    internal static partial void FailedToAppendEvent(this ILogger<EventSequenceStorage> logger, Exception ex, EventSequenceNumber sequenceNumber, EventSequenceId eventSequenceId);

    [LoggerMessage(LogLevel.Warning, "Compensate method called but compensations are not fully implemented yet")]
    internal static partial void CompensateNotImplemented(this ILogger<EventSequenceStorage> logger);

    [LoggerMessage(LogLevel.Warning, "Bulk redaction for event source not implemented yet")]
    internal static partial void BulkRedactionNotImplemented(this ILogger<EventSequenceStorage> logger);

    [LoggerMessage(LogLevel.Warning, "GetTailSequenceNumbers method not implemented yet")]
    internal static partial void GetTailSequenceNumbersNotImplemented(this ILogger<EventSequenceStorage> logger);

    [LoggerMessage(LogLevel.Warning, "GetTailSequenceNumbersForEventTypes method not implemented yet")]
    internal static partial void GetTailSequenceNumbersForEventTypesNotImplemented(this ILogger<EventSequenceStorage> logger);

    [LoggerMessage(LogLevel.Warning, "GetNextSequenceNumberGreaterOrEqualThan method not implemented yet")]
    internal static partial void GetNextSequenceNumberGreaterOrEqualThanNotImplemented(this ILogger<EventSequenceStorage> logger);

    [LoggerMessage(LogLevel.Warning, "TryGetLastEventBefore method not implemented yet")]
    internal static partial void TryGetLastEventBeforeNotImplemented(this ILogger<EventSequenceStorage> logger);

    [LoggerMessage(LogLevel.Warning, "TryGetLastInstanceOfAny method not implemented yet")]
    internal static partial void TryGetLastInstanceOfAnyNotImplemented(this ILogger<EventSequenceStorage> logger);

    [LoggerMessage(LogLevel.Warning, "GetFromSequenceNumber method not implemented yet - EventCursor needed")]
    internal static partial void GetFromSequenceNumberNotImplemented(this ILogger<EventSequenceStorage> logger);

    [LoggerMessage(LogLevel.Warning, "GetRange method not implemented yet - EventCursor needed")]
    internal static partial void GetRangeNotImplemented(this ILogger<EventSequenceStorage> logger);
}
