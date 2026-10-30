// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Observation.Reactors;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Observation.Reactors.Clients;

internal static partial class ReactorLogMessages
{
    [LoggerMessage(LogLevel.Information, "KeepSubscription for reactor observer {ObserverId} (connection {ConnectionId}, silo {SiloAddress}): restore rejected by another transport; waiting for webhook")]
    internal static partial void RestoreRejectedByAnotherTransport(this ILogger<Reactor> logger, ObserverId observerId, ConnectionId connectionId, SiloAddress siloAddress);

    [LoggerMessage(LogLevel.Information, "KeepSubscription for reactor observer {ObserverId} (connection {ConnectionId}, silo {SiloAddress}): completed a deferred registration, nothing more to keep")]
    internal static partial void KeepSubscriptionDeferredRegistration(this ILogger<Reactor> logger, ObserverId observerId, ConnectionId connectionId, SiloAddress siloAddress);

    [LoggerMessage(LogLevel.Information, "KeepSubscription for reactor observer {ObserverId} (connection {ConnectionId}, silo {SiloAddress}): the observer is served by a webhook, returning")]
    internal static partial void KeepSubscriptionServedByWebhook(this ILogger<Reactor> logger, ObserverId observerId, ConnectionId connectionId, SiloAddress siloAddress);

    [LoggerMessage(LogLevel.Information, "KeepSubscription for reactor observer {ObserverId} (connection {ConnectionId}, silo {SiloAddress}): the observer already knows the client, nothing to restore")]
    internal static partial void KeepSubscriptionKnowsClient(this ILogger<Reactor> logger, ObserverId observerId, ConnectionId connectionId, SiloAddress siloAddress);

    [LoggerMessage(LogLevel.Information, "KeepSubscription for reactor observer {ObserverId} (connection {ConnectionId}, silo {SiloAddress}): the client is not connected according to ConnectedClients on this silo, NOT restoring")]
    internal static partial void KeepSubscriptionClientNotConnected(this ILogger<Reactor> logger, ObserverId observerId, ConnectionId connectionId, SiloAddress siloAddress);

    [LoggerMessage(LogLevel.Information, "KeepSubscription for reactor observer {ObserverId} (connection {ConnectionId}, silo {SiloAddress}): the observer does not know the client and it is connected, restoring its subscription")]
    internal static partial void RestoringLostSubscription(this ILogger<Reactor> logger, ObserverId observerId, ConnectionId connectionId, SiloAddress siloAddress);

    [LoggerMessage(LogLevel.Information, "Client subscription for reactor '{ReactorId}' is waiting for the webhook to leave")]
    internal static partial void WaitingForWebhook(this ILogger<Reactor> logger, ReactorId reactorId);

    [LoggerMessage(LogLevel.Debug, "Starting client Reactor {ReactorId} for event store {EventStore} on sequence {EventSequenceId} for namespace {Namespace}")]
    internal static partial void Starting(this ILogger<Reactor> logger, EventStoreName eventStore, ReactorId ReactorId, EventSequenceId eventSequenceId, EventStoreNamespaceName @namespace);

    [LoggerMessage(LogLevel.Information, "Client with connection id {connectionId} has disconnected - unsubscribing observer {ReactorId} for event store {EventStore} on sequence {EventSequenceId} for namespace {Namespace}")]
    internal static partial void ClientDisconnected(this ILogger<Reactor> logger, ConnectionId connectionId, EventStoreName eventStore, ReactorId ReactorId, EventSequenceId eventSequenceId, EventStoreNamespaceName @namespace);

    [LoggerMessage(LogLevel.Information, "Auto-replaying reactor {ReactorId} for event store {EventStore} on sequence {EventSequenceId} for namespace {Namespace} due to definition change")]
    internal static partial void AutoReplayingReactor(this ILogger<Reactor> logger, EventStoreName eventStore, ReactorId ReactorId, EventSequenceId eventSequenceId, EventStoreNamespaceName @namespace);
}
