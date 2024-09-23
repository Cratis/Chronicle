// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Grains.Observation.Reactors.Clients;

internal static partial class ReactorObserverSubscriberLogMessages
{
    [LoggerMessage(LogLevel.Trace, "Reactor {ObserverId} in event store {EventStore} for namespace {Namespace} received event of type {EventTypeId} in sequence {EventSequenceId} with sequence number {EventSequenceNumber}")]
    internal static partial void EventReceived(this ILogger<ReactorObserverSubscriber> logger, ObserverId observerId, EventStoreName eventStore, EventStoreNamespaceName @namespace, EventTypeId eventTypeId, EventSequenceId eventSequenceId, EventSequenceNumber eventSequenceNumber);

    [LoggerMessage(LogLevel.Error, "Reactor {ObserverId} in event store {EventStore} for namespace {Namespace} encountered an error")]
    internal static partial void OnNextException(this ILogger<ReactorObserverSubscriber> logger, Exception ex, ObserverId observerId, EventStoreName eventStore, EventStoreNamespaceName @namespace);
}
