// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation.Reactors;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Observation.Reactors.Clients;

internal static partial class ReactorLogMessages
{
    [LoggerMessage(LogLevel.Debug, "Starting client Reactor {ReactorId} for event store {EventStore} on sequence {EventSequenceId} for namespace {Namespace}")]
    internal static partial void Starting(this ILogger<Reactor> logger, EventStoreName eventStore, ReactorId ReactorId, EventSequenceId eventSequenceId, EventStoreNamespaceName @namespace);

    [LoggerMessage(LogLevel.Debug, "Client with connection id {connectionId} has disconnected - unsubscribing observer {ReactorId} for event store {EventStore} on sequence {EventSequenceId} for namespace {Namespace}")]
    internal static partial void ClientDisconnected(this ILogger<Reactor> logger, ConnectionId connectionId, EventStoreName eventStore, ReactorId ReactorId, EventSequenceId eventSequenceId, EventStoreNamespaceName @namespace);
}
