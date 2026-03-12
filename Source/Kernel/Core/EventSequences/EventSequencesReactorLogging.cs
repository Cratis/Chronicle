// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.EventSequences;

internal static partial class EventSequencesReactorLogging
{
    [LoggerMessage(LogLevel.Information, "Redacting event @ {SequenceNumber} in event sequence '{EventSequenceId}' for event store '{EventStore}' on namespace '{Namespace}'")]
    internal static partial void Redacting(this ILogger<EventSequencesReactor> logger, EventStoreName eventStore, EventStoreNamespaceName @namespace, EventSequenceId eventSequenceId, EventSequenceNumber sequenceNumber);

    [LoggerMessage(LogLevel.Information, "Redacting events for event source {EventSourceId} and event types {EventTypes} in event sequence '{EventSequenceId}' for event store '{EventStore}' on namespace '{Namespace}'")]
    internal static partial void RedactingForEventSource(this ILogger<EventSequencesReactor> logger, EventStoreName eventStore, EventStoreNamespaceName @namespace, EventSequenceId eventSequenceId, EventSourceId eventSourceId, IEnumerable<EventType> eventTypes);

    [LoggerMessage(LogLevel.Information, "Compensating event @ {SequenceNumber} in event sequence '{EventSequenceId}' with event type '{EventType}' for event store '{EventStore}' on namespace '{Namespace}'")]
    internal static partial void Compensating(this ILogger<EventSequencesReactor> logger, EventStoreName eventStore, EventStoreNamespaceName @namespace, EventSequenceId eventSequenceId, EventSequenceNumber sequenceNumber, EventType eventType);
}
