// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Grains.EventSequences;

/// <summary>
/// Holds log messages for <see cref="EventSequence"/>.
/// </summary>
internal static partial class EventSequenceLogMessages
{
    [LoggerMessage(LogLevel.Debug, "Appending '{EventType}' for EventSourceId {EventSourceId} with sequence number {SequenceNumber} to event sequence '{EventSequenceId} for event store {EventStore} on namespace {Namespace}")]
    internal static partial void Appending(this ILogger<EventSequence> logger, EventStoreName eventStore, EventStoreNamespaceName @namespace, EventSequenceId eventSequenceId, EventType eventType, EventSourceId eventSourceId, EventSequenceNumber sequenceNumber);

    [LoggerMessage(LogLevel.Debug, "Duplicate  '{EventType}' for EventSourceId {EventSourceId} with sequence number {SequenceNumber} to event sequence '{EventSequenceId} for event store {EventStore} on namespace {Namespace}")]
    internal static partial void DuplicateEvent(this ILogger<EventSequence> logger, EventStoreName eventStore, EventStoreNamespaceName @namespace, EventSequenceId eventSequenceId, EventType eventType, EventSourceId eventSourceId, EventSequenceNumber sequenceNumber);

    [LoggerMessage(LogLevel.Debug, "Compensating event @ {SequenceNumber} in event sequence {EventSequenceId} - event type '{EventType}' for event store '{EventStore}' on namespace {Namespace}")]
    internal static partial void Compensating(this ILogger<EventSequence> logger, EventStoreName eventStore, EventStoreNamespaceName @namespace, EventType eventType, EventSequenceId eventSequenceId, EventSequenceNumber sequenceNumber);

    [LoggerMessage(LogLevel.Error, "Failed appending event type '{EventType}' at sequence {SequenceNumber} for event source type {EventSourceType} and event source id {EventSourceId} to stream {EventSequenceId} for event store '{EventStore}' on namespace {Namespace}")]
    internal static partial void ErrorAppending(this ILogger<EventSequence> logger, EventStoreName eventStore, EventStoreNamespaceName @namespace, EventType eventType, EventSequenceId eventSequenceId, EventSourceType eventSourceType, EventSourceId eventSourceId, EventSequenceNumber sequenceNumber, Exception exception);

    [LoggerMessage(LogLevel.Information, "Redacting event @ {SequenceNumber} in event sequence {EventSequenceId} for event store '{EventStore}' on namespace {Namespace}")]
    internal static partial void Redacting(this ILogger<EventSequence> logger, EventStoreName eventStore, EventStoreNamespaceName @namespace, EventSequenceId eventSequenceId, EventSequenceNumber sequenceNumber);

    [LoggerMessage(LogLevel.Information, "Redacting events with event source id {EventSourceId} and event types {EventTypes} in event sequence {EventSequenceId} for event store '{EventStore}' on namespace {Namespace}")]
    internal static partial void RedactingMultiple(this ILogger<EventSequence> logger, EventStoreName eventStore, EventStoreNamespaceName @namespace, EventSequenceId eventSequenceId, EventSourceId eventSourceId, IEnumerable<EventType> eventTypes);

    [LoggerMessage(LogLevel.Critical, "A critical error occurred while appending many events after successfully validating all event constraints. One or more events was committed while they should have been rolled back and not been written to the event store")]
    internal static partial void FailedDuringAppendingManyEvents(this ILogger<EventSequence> logger);

    [LoggerMessage(LogLevel.Debug, "Getting tail sequence number for event types from event sequence {EventSequenceId} for event store '{EventStore}' on namespace {Namespace} for event types {EventTypes}")]
    internal static partial void GettingTailSequenceNumberForEventTypes(this ILogger<EventSequence> logger, EventStoreName eventStore, EventStoreNamespaceName @namespace, EventSequenceId eventSequenceId, IEnumerable<EventType> eventTypes);

    [LoggerMessage(LogLevel.Debug, "Sequence number is {SequenceNumber} when getting tail sequence number for event types from event sequence {EventSequenceId} for event store '{EventStore}' on namespace {Namespace} for event types {EventTypes}")]
    internal static partial void ResultForGettingTailSequenceNumberForEventTypes(this ILogger<EventSequence> logger, EventStoreName eventStore, EventStoreNamespaceName @namespace, EventSequenceId eventSequenceId, IEnumerable<EventType> eventTypes, EventSequenceNumber sequenceNumber);

    [LoggerMessage(LogLevel.Error, "Failed getting tail sequence number for event types from event sequence {EventSequenceId} for event store '{EventStore}' on namespace {Namespace} for event types {EventTypes}")]
    internal static partial void FailedGettingTailSequenceNumberForEventTypes(this ILogger<EventSequence> logger, EventStoreName eventStore, EventStoreNamespaceName @namespace, EventSequenceId eventSequenceId, IEnumerable<EventType> eventTypes, Exception exception);

    [LoggerMessage(LogLevel.Debug, "Getting tail sequence number greater or equal than {SequenceNumber} for event types from event sequence {EventSequenceId} for event store '{EventStore}' on namespace {Namespace} for event types {EventTypes}")]
    internal static partial void GettingNextSequenceNumberGreaterOrEqualThan(this ILogger<EventSequence> logger, EventStoreName eventStore, EventStoreNamespaceName @namespace, EventSequenceId eventSequenceId, EventSequenceNumber sequenceNumber, IEnumerable<EventType> eventTypes);

    [LoggerMessage(LogLevel.Error, "Failed getting tail sequence number greater or equal than {SequenceNumber} for event types from event sequence {EventSequenceId} for event store '{EventStore}' on namespace {Namespace} for event types {EventTypes}")]
    internal static partial void FailedGettingNextSequenceNumberGreaterOrEqualThan(this ILogger<EventSequence> logger, EventStoreName eventStore, EventStoreNamespaceName @namespace, EventSequenceId eventSequenceId, EventSequenceNumber sequenceNumber, IEnumerable<EventType> eventTypes, Exception exception);

    [LoggerMessage(LogLevel.Debug, "Sequence number is {Result} when getting tail sequence number greater or equal than {SequenceNumber} for event types from event sequence {EventSequenceId} for event store '{EventStore}' on namespace {Namespace} for event types {EventTypes}")]
    internal static partial void NextSequenceNumberGreaterOrEqualThan(this ILogger<EventSequence> logger, EventStoreName eventStore, EventStoreNamespaceName @namespace, EventSequenceId eventSequenceId, EventSequenceNumber sequenceNumber, IEnumerable<EventType> eventTypes, EventSequenceNumber result);
}
