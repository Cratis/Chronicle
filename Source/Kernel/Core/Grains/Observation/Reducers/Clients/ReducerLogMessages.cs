// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Grains.Observation.Reducers.Clients;

internal static partial class ReducerLogMessages
{
    [LoggerMessage(LogLevel.Information, "Starting client reducer {observerId} for event store {EventStore} on sequence {EventSequenceId} for namespace {@namespace}")]
    internal static partial void Starting(this ILogger<Reducer> logger, EventStoreName eventStore, ObserverId observerId, EventSequenceId eventSequenceId, EventStoreNamespaceName @namespace);

    [LoggerMessage(LogLevel.Information, "Client with connection id {connectionId} has disconnected - unsubscribing reducer {observerId} for event store {EventStore} on sequence {EventSequenceId} for namespace {@namespace}")]
    internal static partial void ClientDisconnected(this ILogger<Reducer> logger, ConnectionId connectionId, EventStoreName eventStore, ObserverId observerId, EventSequenceId eventSequenceId, EventStoreNamespaceName @namespace);
}
