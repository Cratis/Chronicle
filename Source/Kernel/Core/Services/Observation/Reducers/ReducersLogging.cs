// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Observation.Reducers;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Services.Observation.Reducers;

#pragma warning disable SA1600 // Elements should be documented
#pragma warning disable MA0048 // File name must match type name
#pragma warning disable SA1402 // File may only contain a single type

internal static partial class ReducersLogMessages
{
    [LoggerMessage(LogLevel.Debug, "Reducer observe method invoked")]
    internal static partial void Observe(this ILogger<Reducers> logger);

    [LoggerMessage(LogLevel.Debug, "Registering Reducer {ReducerId} from connection {ConnectionId} with event store {EventStore} and namespace {@Namespace} for event sequence {EventSequenceId}")]
    internal static partial void Registering(this ILogger<Reducers> logger, ReducerId reducerId, Concepts.EventStoreName eventStore, Concepts.EventStoreNamespaceName @namespace, EventSequenceId eventSequenceId, ConnectionId connectionId);

    [LoggerMessage(LogLevel.Debug, "Subscribing reducer {ReducerId} from connection {ConnectionId} with event store {EventStore} and namespace {@Namespace} for event sequence {EventSequenceId}")]
    internal static partial void Subscribing(this ILogger<Reducers> logger, ReducerId reducerId, Concepts.EventStoreName eventStore, Concepts.EventStoreNamespaceName @namespace, EventSequenceId eventSequenceId, ConnectionId connectionId);

    [LoggerMessage(LogLevel.Debug, "Reducer {ReducerId} disconnected from connection {ConnectionId} with event store {EventStore} and namespace {@Namespace} for event sequence {EventSequenceId}")]
    internal static partial void Disconnected(this ILogger<Reducers> logger, ReducerId reducerId, Concepts.EventStoreName eventStore, Concepts.EventStoreNamespaceName @namespace, EventSequenceId eventSequenceId, ConnectionId connectionId);

    [LoggerMessage(LogLevel.Debug, "Reducer {ReducerId} observer stream disconnected from connection {ConnectionId}")]
    internal static partial void ObserverStreamDisconnected(this ILogger<Reducers> logger, ObserverId reducerId, ConnectionId connectionId);

    [LoggerMessage(LogLevel.Warning, "Reducer {ReducerId} disconnected ungracefully from connection {ConnectionId}")]
    internal static partial void DisconnectedUngracefully(this ILogger<Reducers> logger, ObserverId reducerId, ConnectionId connectionId);

    [LoggerMessage(LogLevel.Error, "Error occurred for reducer {ReducerId} from connection {ConnectionId} - disengaging")]
    internal static partial void Disengage(this ILogger<Reducers> logger, ObserverId reducerId, ConnectionId connectionId, Exception exception);
}
