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
    [LoggerMessage(LogLevel.Information, "Reactor activation {ObserverKey} for connection {ConnectionId} is on silo {ActivationSilo}; connected-client registry key is this silo address")]
    internal static partial void ActivationRegistry(this ILogger<Reactor> logger, string observerKey, ConnectionId connectionId, string activationSilo);

    [LoggerMessage(LogLevel.Information, "Reactor {ObserverKey} resolved requested connection {ConnectionId} to registry client {ResolvedConnectionId} on silo/registry key {RegistryKey}; subscription target is this silo")]
    internal static partial void RegistryClientResolved(this ILogger<Reactor> logger, string observerKey, ConnectionId connectionId, ConnectionId resolvedConnectionId, string registryKey);

    [LoggerMessage(LogLevel.Warning, "Reactor {ObserverKey} cannot restore connection {ConnectionId}: observer does not know client and IsConnected is false on silo/registry key {RegistryKey}; local subscribed flag is {Subscribed}. Registry diagnostic outcome changed")]
    internal static partial void RegistryClientMissing(this ILogger<Reactor> logger, string observerKey, ConnectionId connectionId, string registryKey, bool subscribed);

    [LoggerMessage(LogLevel.Debug, "Starting client Reactor {ReactorId} for event store {EventStore} on sequence {EventSequenceId} for namespace {Namespace}")]
    internal static partial void Starting(this ILogger<Reactor> logger, EventStoreName eventStore, ReactorId ReactorId, EventSequenceId eventSequenceId, EventStoreNamespaceName @namespace);

    [LoggerMessage(LogLevel.Debug, "Client with connection id {connectionId} has disconnected - unsubscribing observer {ReactorId} for event store {EventStore} on sequence {EventSequenceId} for namespace {Namespace}")]
    internal static partial void ClientDisconnected(this ILogger<Reactor> logger, ConnectionId connectionId, EventStoreName eventStore, ReactorId ReactorId, EventSequenceId eventSequenceId, EventStoreNamespaceName @namespace);

    [LoggerMessage(LogLevel.Information, "Auto-replaying reactor {ReactorId} for event store {EventStore} on sequence {EventSequenceId} for namespace {Namespace} due to definition change")]
    internal static partial void AutoReplayingReactor(this ILogger<Reactor> logger, EventStoreName eventStore, ReactorId ReactorId, EventSequenceId eventSequenceId, EventStoreNamespaceName @namespace);
}
