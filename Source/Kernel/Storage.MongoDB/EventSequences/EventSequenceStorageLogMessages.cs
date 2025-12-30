// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences;

internal static partial class EventSequenceStorageLogMessages
{
    [LoggerMessage(LogLevel.Debug, "EventSequenceStorage.AppendMany inserting {Count} events for {Sequence}")]
    internal static partial void AppendingInserting(this ILogger<EventSequenceStorage> logger, int count, EventSequenceId sequence);

    [LoggerMessage(LogLevel.Debug, "EventSequenceStorage.AppendMany inserted {Count} events and prepared {AppendedCount} appended events for {Sequence}")]
    internal static partial void AppendingInserted(this ILogger<EventSequenceStorage> logger, int count, int appendedCount, EventSequenceId sequence);

    [LoggerMessage(LogLevel.Debug, "Getting head sequence number for event sequence {EventSequenceId}")]
    internal static partial void GettingHeadSequenceNumber(this ILogger<EventSequenceStorage> logger, EventSequenceId eventSequenceId);

    [LoggerMessage(LogLevel.Debug, "Getting tail sequence number for event sequence {EventSequenceId}")]
    internal static partial void GettingTailSequenceNumber(this ILogger<EventSequenceStorage> logger, EventSequenceId eventSequenceId);

    [LoggerMessage(LogLevel.Debug, "Getting last instance of event of type '{EventTypeId}' and event source id '{EventSourceId}' in event sequence {EventSequenceId}")]
    internal static partial void GettingLastInstanceFor(this ILogger<EventSequenceStorage> logger, EventSequenceId eventSequenceId, EventTypeId eventTypeId, EventSourceId eventSourceId);

    [LoggerMessage(LogLevel.Debug, "Getting last instance of event of types ['{EventTypes}'] and event source id '{EventSourceId}' in event sequence {EventSequenceId}")]
    internal static partial void GettingLastInstanceOfAny(this ILogger<EventSequenceStorage> logger, EventSequenceId eventSequenceId, EventSourceId eventSourceId, IEnumerable<EventTypeId> eventTypes);

    [LoggerMessage(LogLevel.Debug, "Getting events from sequence number {From} in event sequence {EventSequenceId}")]
    internal static partial void GettingFromSequenceNumber(this ILogger<EventSequenceStorage> logger, EventSequenceId eventSequenceId, EventSequenceNumber from);

    [LoggerMessage(LogLevel.Debug, "Getting range of events from {From} to {To} in event sequence {EventSequenceId}")]
    internal static partial void GettingRange(this ILogger<EventSequenceStorage> logger, EventSequenceId eventSequenceId, EventSequenceNumber from, EventSequenceNumber to);

    [LoggerMessage(LogLevel.Information, "Redacting event with sequence number {EventSequenceNumber} from sequence {EventSequenceId}")]
    internal static partial void Redacting(this ILogger<EventSequenceStorage> logger, EventSequenceId eventSequenceId, EventSequenceNumber eventSequenceNumber);

    [LoggerMessage(LogLevel.Information, "Redacting events with event source id {EventSourceId} and event types {EventTypes} from sequence {EventSequenceId}")]
    internal static partial void RedactingMultiple(this ILogger<EventSequenceStorage> logger, EventSequenceId eventSequenceId, EventSourceId eventSourceId, IEnumerable<EventType> eventTypes);

    [LoggerMessage(LogLevel.Debug, "Getting instance of event at sequence number {EventSequenceNumber} in event sequence {EventSequenceId}")]
    internal static partial void GettingEventAtSequenceNumber(this ILogger<EventSequenceStorage> logger, EventSequenceId eventSequenceId, EventSequenceNumber eventSequenceNumber);

    [LoggerMessage(LogLevel.Warning, "Redaction of event with sequence number {EventSequenceNumber} on sequence {EventSequenceId} has already been performed. Ignoring.")]
    internal static partial void RedactionAlreadyApplied(this ILogger<EventSequenceStorage> logger, EventSequenceId eventSequenceId, EventSequenceNumber eventSequenceNumber);

    [LoggerMessage(LogLevel.Debug, "Getting tail sequence number for event sequence {EventSequenceId} for event types {EventTypes}")]
    internal static partial void GettingTailSequenceNumbersForEventTypes(this ILogger<EventSequenceStorage> logger, EventSequenceId eventSequenceId, IEnumerable<string> eventTypes);
}
