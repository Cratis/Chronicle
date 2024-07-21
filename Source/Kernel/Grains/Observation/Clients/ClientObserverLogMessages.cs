// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Reactions;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Grains.Observation.Clients;

internal static partial class ClientObserverLogMessages
{
    [LoggerMessage(LogLevel.Debug, "Starting client observer {observerId} for event store {EventStore} on sequence {EventSequenceId} for namespace {Namespace}")]
    internal static partial void Starting(this ILogger<ClientObserver> logger, EventStoreName eventStore, ObserverId observerId, EventSequenceId eventSequenceId, EventStoreNamespaceName @namespace);

    [LoggerMessage(LogLevel.Debug, "Client with connection id {connectionId} has disconnected - unsubscribing observer {observerId} for event store {EventStore} on sequence {EventSequenceId} for namespace {Namespace}")]
    internal static partial void ClientDisconnected(this ILogger<ClientObserver> logger, ConnectionId connectionId, EventStoreName eventStore, ObserverId observerId, EventSequenceId eventSequenceId, EventStoreNamespaceName @namespace);
}
