// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Observation;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Grains.Observation.Reactions.Clients;

internal static partial class ReactionSubscriberLogMessages
{
    [LoggerMessage(LogLevel.Trace, "Observer {ObserverId} in event store {EventStore} for namespace {Namespace} received event of type {EventTypeId} in sequence {EventSequenceId} with sequence number {EventSequenceNumber}")]
    internal static partial void EventReceived(this ILogger<ReactionSubscriber> logger, ObserverId observerId, EventStoreName eventStore, EventStoreNamespaceName @namespace, EventTypeId eventTypeId, EventSequenceId eventSequenceId, EventSequenceNumber eventSequenceNumber);
}
