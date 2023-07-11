// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events;
using Aksio.Cratis.EventSequences;
using Microsoft.Extensions.Logging;

namespace Aksio.Cratis.Kernel.Grains.EventSequences;

/// <summary>
/// Holds log messages for <see cref="EventSequence"/>.
/// </summary>
internal static partial class EventSequenceLogMessages
{
    [LoggerMessage(0, LogLevel.Debug, "Appending '{EventName}-{EventType}' for EventSource {EventSource} with sequence number {SequenceNumber} to event sequence '{EventSequenceId} for microservice {MicroserviceId} on tenant {TenantId}")]
    internal static partial void Appending(this ILogger<EventSequence> logger, MicroserviceId microserviceId, TenantId tenantId, EventSequenceId eventSequenceId, EventType eventType, string eventName, EventSourceId eventSource, EventSequenceNumber sequenceNumber);

    [LoggerMessage(1, LogLevel.Debug, "Compensating event @ {SequenceNumber} in event sequence {EventSequenceId} - event type '{EventType}' for microservice '{MicroserviceId}' on tenant {TenantId}")]
    internal static partial void Compensating(this ILogger<EventSequence> logger, MicroserviceId microserviceId, TenantId tenantId, EventType eventType, EventSequenceId eventSequenceId, EventSequenceNumber sequenceNumber);

    [LoggerMessage(2, LogLevel.Critical, "Failed appending event type '{EventType}' at sequence {SequenceNumber} for event source {EventSourceId} to stream {StreamId} for microservice '{MicroserviceId}' on tenant {TenantId}")]
    internal static partial void FailedAppending(this ILogger<EventSequence> logger, MicroserviceId microserviceId, TenantId tenantId, EventType eventType, Guid streamId, EventSourceId eventSourceId, EventSequenceNumber sequenceNumber, Exception exception);

    [LoggerMessage(3, LogLevel.Error, "Error when appending event at sequence {SequenceNumber} for event source {EventSourceId} to event sequence {EventSequenceId} for microservice {MicroserviceId} on tenant {TenantId}")]
    internal static partial void ErrorAppending(this ILogger<EventSequence> logger, MicroserviceId microserviceId, TenantId tenantId, EventSequenceId eventSequenceId, EventSourceId eventSourceId, EventSequenceNumber sequenceNumber, Exception exception);

    [LoggerMessage(4, LogLevel.Information, "Redacting event @ {SequenceNumber} in event sequence {EventSequenceId} for microservice '{MicroserviceId}' on tenant {TenantId}")]
    internal static partial void Redacting(this ILogger<EventSequence> logger, MicroserviceId microserviceId, TenantId tenantId, EventSequenceId eventSequenceId, EventSequenceNumber sequenceNumber);

    [LoggerMessage(5, LogLevel.Information, "Redacting events with event source id {EventSourceId} and event types {EventTypes} in event sequence {EventSequenceId} for microservice '{MicroserviceId}' on tenant {TenantId}")]
    internal static partial void RedactingMultiple(this ILogger<EventSequence> logger, MicroserviceId microserviceId, TenantId tenantId, EventSequenceId eventSequenceId, EventSourceId eventSourceId, IEnumerable<EventType> eventTypes);
}
