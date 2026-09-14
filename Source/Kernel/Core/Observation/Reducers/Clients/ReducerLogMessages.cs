// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Observation.Reducers.Clients;

internal static partial class ReducerLogMessages
{
    [LoggerMessage(LogLevel.Debug, "Starting client reducer {observerId} for event store {EventStore} on sequence {EventSequenceId} for namespace {@namespace}")]
    internal static partial void Starting(this ILogger<Reducer> logger, EventStoreName eventStore, ObserverId observerId, EventSequenceId eventSequenceId, EventStoreNamespaceName @namespace);

    [LoggerMessage(LogLevel.Debug, "Client with connection id {connectionId} has disconnected - unsubscribing reducer {observerId} for event store {EventStore} on sequence {EventSequenceId} for namespace {@namespace}")]
    internal static partial void ClientDisconnected(this ILogger<Reducer> logger, ConnectionId connectionId, EventStoreName eventStore, ObserverId observerId, EventSequenceId eventSequenceId, EventStoreNamespaceName @namespace);

    [LoggerMessage(LogLevel.Information, "Full replay - reducer {observerId} for event store {EventStore} on sequence {EventSequenceId} in namespace {@namespace} changed in a way that may affect existing read models")]
    internal static partial void AutoReplayingReducer(this ILogger<Reducer> logger, EventStoreName eventStore, ObserverId observerId, EventSequenceId eventSequenceId, EventStoreNamespaceName @namespace);

    [LoggerMessage(LogLevel.Information, "Partial replay - reducer {observerId} for event store {EventStore} on sequence {EventSequenceId} in namespace {@namespace} only affects {AffectedEventSourceCount} event sources")]
    internal static partial void PartiallyReplayingReducer(this ILogger<Reducer> logger, EventStoreName eventStore, ObserverId observerId, EventSequenceId eventSequenceId, EventStoreNamespaceName @namespace, int affectedEventSourceCount);

    [LoggerMessage(LogLevel.Information, "No action - reducer {observerId} for event store {EventStore} on sequence {EventSequenceId} in namespace {@namespace} only consumes newly added event types with no historical events")]
    internal static partial void ReducerEvolutionNeedsNoAction(this ILogger<Reducer> logger, EventStoreName eventStore, ObserverId observerId, EventSequenceId eventSequenceId, EventStoreNamespaceName @namespace);
}
