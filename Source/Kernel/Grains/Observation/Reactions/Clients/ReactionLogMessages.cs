// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation.Reactions;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Grains.Observation.Reactions.Clients;

internal static partial class ReactionLogMessages
{
    [LoggerMessage(LogLevel.Debug, "Starting client reaction {reactionId} for event store {EventStore} on sequence {EventSequenceId} for namespace {Namespace}")]
    internal static partial void Starting(this ILogger<Reaction> logger, EventStoreName eventStore, ReactionId reactionId, EventSequenceId eventSequenceId, EventStoreNamespaceName @namespace);

    [LoggerMessage(LogLevel.Debug, "Client with connection id {connectionId} has disconnected - unsubscribing observer {reactionId} for event store {EventStore} on sequence {EventSequenceId} for namespace {Namespace}")]
    internal static partial void ClientDisconnected(this ILogger<Reaction> logger, ConnectionId connectionId, EventStoreName eventStore, ReactionId reactionId, EventSequenceId eventSequenceId, EventStoreNamespaceName @namespace);
}
