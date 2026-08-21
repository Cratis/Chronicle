// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Observation.Reactors;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Services.Observation.Reactors;

#pragma warning disable SA1600 // Elements should be documented
#pragma warning disable MA0048 // File name must match type name
#pragma warning disable SA1402 // File may only contain a single type

internal static partial class ReactorsLogMessages
{
    [LoggerMessage(LogLevel.Debug, "Reactor observe method invoked")]
    internal static partial void Observe(this ILogger<Reactors> logger);

    [LoggerMessage(LogLevel.Debug, "Registering Reactor {ReactorId} from connection {ConnectionId} with event store {EventStore} and namespace {@Namespace} for event sequence {EventSequenceId}")]
    internal static partial void Registering(this ILogger<Reactors> logger, ReactorId reactorId, Concepts.EventStoreName eventStore, Concepts.EventStoreNamespaceName @namespace, EventSequenceId eventSequenceId, ConnectionId connectionId);

    [LoggerMessage(LogLevel.Debug, "Subscribing reactor {ReactorId} from connection {ConnectionId} with event store {EventStore} and namespace {@Namespace} for event sequence {EventSequenceId}")]
    internal static partial void Subscribing(this ILogger<Reactors> logger, ReactorId reactorId, Concepts.EventStoreName eventStore, Concepts.EventStoreNamespaceName @namespace, EventSequenceId eventSequenceId, ConnectionId connectionId);

    [LoggerMessage(LogLevel.Debug, "Reactor {ReactorId} disconnected from connection {ConnectionId} with event store {EventStore} and namespace {@Namespace} for event sequence {EventSequenceId}")]
    internal static partial void Disconnected(this ILogger<Reactors> logger, ReactorId reactorId, Concepts.EventStoreName eventStore, Concepts.EventStoreNamespaceName @namespace, EventSequenceId eventSequenceId, ConnectionId connectionId);

    [LoggerMessage(LogLevel.Debug, "Reactor {ReactorId} observer stream disconnected from connection {ConnectionId}")]
    internal static partial void ObserverStreamDisconnected(this ILogger<Reactors> logger, ObserverId reactorId, ConnectionId connectionId);

    [LoggerMessage(LogLevel.Warning, "Reactor {ReactorId} disconnected ungracefully from connection {ConnectionId}")]
    internal static partial void DisconnectedUngracefully(this ILogger<Reactors> logger, ObserverId reactorId, ConnectionId connectionId);

    [LoggerMessage(LogLevel.Error, "Error occurred for reactor {ReactorId} from connection {ConnectionId} - disengaging")]
    internal static partial void Disengage(this ILogger<Reactors> logger, ObserverId reactorId, ConnectionId connectionId, Exception exception);
}
