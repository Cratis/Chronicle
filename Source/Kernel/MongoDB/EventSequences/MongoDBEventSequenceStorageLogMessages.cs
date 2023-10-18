// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events;
using Aksio.Cratis.EventSequences;
using Microsoft.Extensions.Logging;

namespace Aksio.Cratis.Kernel.MongoDB.EventSequences;

internal static partial class MongoDBEventSequenceStorageLogMessages
{
    [LoggerMessage(0, LogLevel.Trace, "Appending event with '{SequenceNumber}' as sequence number for sequence '{EventSequenceId}' in microservice '{MicroserviceId}' for tenant '{TenantId}'")]
    internal static partial void Appending(this ILogger<MongoDBEventSequenceStorage> logger, ulong sequenceNumber, EventSequenceId eventSequenceId, MicroserviceId microserviceId, TenantId tenantId);

    [LoggerMessage(1, LogLevel.Error, "Problem appending event with '{SequenceNumber}' as sequence number for sequence '{EventSequenceId}' in microservice '{MicroserviceId}' for tenant '{TenantId}'")]
    internal static partial void AppendFailure(this ILogger<MongoDBEventSequenceStorage> logger, ulong sequenceNumber, EventSequenceId eventSequenceId, MicroserviceId microserviceId, TenantId tenantId, Exception exception);

    [LoggerMessage(2, LogLevel.Warning, "Duplicate event sequence number '{SequenceNumber}' when appending for sequence '{EventSequenceId}' in microservice '{MicroserviceId}' for tenant '{TenantId}'")]
    internal static partial void DuplicateEventSequenceNumber(this ILogger<MongoDBEventSequenceStorage> logger, ulong sequenceNumber, EventSequenceId eventSequenceId, MicroserviceId microserviceId, TenantId tenantId);

    [LoggerMessage(3, LogLevel.Debug, "Getting head sequence number for event sequence {EventSequenceId}")]
    internal static partial void GettingHeadSequenceNumber(this ILogger<MongoDBEventSequenceStorage> logger, EventSequenceId eventSequenceId);

    [LoggerMessage(4, LogLevel.Debug, "Getting tail sequence number for event sequence {EventSequenceId}")]
    internal static partial void GettingTailSequenceNumber(this ILogger<MongoDBEventSequenceStorage> logger, EventSequenceId eventSequenceId);

    [LoggerMessage(5, LogLevel.Debug, "Getting last instance of event of type '{EventTypeId}' and event source id '{EventSourceId}' in event sequence {EventSequenceId}")]
    internal static partial void GettingLastInstanceFor(this ILogger<MongoDBEventSequenceStorage> logger, EventSequenceId eventSequenceId, EventTypeId eventTypeId, EventSourceId eventSourceId);

    [LoggerMessage(6, LogLevel.Debug, "Getting last instance of event of types ['{EventTypes}'] and event source id '{EventSourceId}' in event sequence {EventSequenceId}")]
    internal static partial void GettingLastInstanceOfAny(this ILogger<MongoDBEventSequenceStorage> logger, EventSequenceId eventSequenceId, EventSourceId eventSourceId, IEnumerable<EventTypeId> eventTypes);

    [LoggerMessage(7, LogLevel.Debug, "Getting events from sequence number {From} in event sequence {EventSequenceId}")]
    internal static partial void GettingFromSequenceNumber(this ILogger<MongoDBEventSequenceStorage> logger, EventSequenceId eventSequenceId, EventSequenceNumber from);

    [LoggerMessage(8, LogLevel.Debug, "Getting range of events from {From} to {To} in event sequence {EventSequenceId}")]
    internal static partial void GettingRange(this ILogger<MongoDBEventSequenceStorage> logger, EventSequenceId eventSequenceId, EventSequenceNumber from, EventSequenceNumber to);

    [LoggerMessage(9, LogLevel.Information, "Redacting event with sequence number {EventSequenceNumber} from sequence {EventSequenceId}")]
    internal static partial void Redacting(this ILogger<MongoDBEventSequenceStorage> logger, EventSequenceId eventSequenceId, EventSequenceNumber eventSequenceNumber);

    [LoggerMessage(10, LogLevel.Information, "Redacting events with event source id {EventSourceId} and event types {EventTypes} from sequence {EventSequenceId}")]
    internal static partial void RedactingMultiple(this ILogger<MongoDBEventSequenceStorage> logger, EventSequenceId eventSequenceId, EventSourceId eventSourceId, IEnumerable<EventType> eventTypes);

    [LoggerMessage(11, LogLevel.Debug, "Getting instance of event at sequence number {EventSequenceNumber} in event sequence {EventSequenceId}")]
    internal static partial void GettingEventAtSequenceNumber(this ILogger<MongoDBEventSequenceStorage> logger, EventSequenceId eventSequenceId, EventSequenceNumber eventSequenceNumber);

    [LoggerMessage(12, LogLevel.Warning, "Redaction of event with sequence number {EventSequenceNumber} on sequence {EventSequenceId} has already been performed. Ignoring.")]
    internal static partial void RedactionAlreadyApplied(this ILogger<MongoDBEventSequenceStorage> logger, EventSequenceId eventSequenceId, EventSequenceNumber eventSequenceNumber);

    [LoggerMessage(13, LogLevel.Debug, "Getting tail sequence number for event sequence {EventSequenceId} for event types {EventTypes}")]
    internal static partial void GettingTailSequenceNumbers(this ILogger<MongoDBEventSequenceStorage> logger, EventSequenceId eventSequenceId, IEnumerable<EventTypeId> eventTypes);
}
