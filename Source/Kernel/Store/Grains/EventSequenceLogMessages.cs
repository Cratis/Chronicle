// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Execution;
using Microsoft.Extensions.Logging;

namespace Aksio.Cratis.Events.Store.Grains;

/// <summary>
/// Holds log messages for <see cref="EventSequence"/>.
/// </summary>
public static partial class EventSequenceLogMessages
{
    [LoggerMessage(0, LogLevel.Information, "Appending '{EventType}' for EventSource {EventSource} with sequence number {SequenceNumber} to event sequence '{EventSequenceId} for microservice {MicroserviceId} on tenant {TenantId}")]
    internal static partial void Appending(this ILogger logger, MicroserviceId microserviceId, TenantId tenantId, EventSequenceId eventSequenceId, EventType eventType, EventSourceId eventSource, ulong sequenceNumber);

    [LoggerMessage(1, LogLevel.Information, "Compensatin event @ {SequenceNumber} in event sequence {EventSequenceId} - event type {EventType} for microservice '{MicroserviceId}' on tenant {TenantId}")]
    internal static partial void Compensating(this ILogger logger, MicroserviceId microserviceId, TenantId tenantId, EventType eventTYpe, EventSequenceId eventSequenceId, ulong sequenceNumber);

    [LoggerMessage(2, LogLevel.Critical, "Failed appending event at sequence {SequenceNumber} for event source {EventSourceId} to stream {StreamId} for microservice '{MicroserviceId}' on tenant {TenantId}")]
    internal static partial void FailedAppending(this ILogger logger, MicroserviceId microserviceId, TenantId tenantId, Guid streamId, string eventSourceId, ulong sequenceNumber, Exception exception);

    [LoggerMessage(3, LogLevel.Error, "Error when appending event at sequence {SequenceNumber} for event source {EventSourceId} to event sequence {EventSequenceId} for microservice {MicroserviceId} on tenant {TenantId}")]
    internal static partial void ErrorAppending(this ILogger logger, MicroserviceId microserviceId, TenantId tenantId, EventSequenceId eventSequenceId, string eventSourceId, ulong sequenceNumber, Exception exception);
}
