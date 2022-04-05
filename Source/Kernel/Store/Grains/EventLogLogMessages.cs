// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Aksio.Cratis.Events.Store.Grains;

/// <summary>
/// Holds log messages for <see cref="EventLog"/>.
/// </summary>
public static partial class EventLogLogMessages
{
    [LoggerMessage(0, LogLevel.Information, "Appending '{EventType}' for EventSource '{EventSource}' with sequence number {SequenceNumber} to event log '{EventLog}")]
    internal static partial void Appending(this ILogger logger, EventType eventType, EventSourceId eventSource, ulong sequenceNumber, EventLogId eventLog);

    [LoggerMessage(1, LogLevel.Information, "Compensatin event @ {SequenceNumber} in event log '{EventLog}' - event type '{EventType}'")]
    internal static partial void Compensating(this ILogger logger, EventType eventTYpe, ulong sequenceNumber, EventLogId eventLog);

    [LoggerMessage(2, LogLevel.Critical, "Failed appending event at sequence {SequenceNumber} for event source {EventSourceId} on tenant {TenantId} from stream {StreamId}")]
    internal static partial void FailedAppending(this ILogger logger, Guid streamId, ulong sequenceNumber, Guid tenantId, string eventSourceId, Exception exception);

    [LoggerMessage(3, LogLevel.Error, "Error when appending event at sequence {SequenceNumber} for event source {EventSourceId} on tenant {TenantId} from stream {EventLogId}")]
    internal static partial void ErrorAppending(this ILogger logger, EventLogId eventLogId, ulong sequenceNumber, Guid tenantId, string eventSourceId, Exception exception);
}
