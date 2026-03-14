// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Observation.EventStoreSubscriptions;

/// <summary>
/// Holds log messages for <see cref="EventStoreSubscriptionObserverSubscriber"/>.
/// </summary>
internal static partial class EventStoreSubscriptionObserverSubscriberLogging
{
    [LoggerMessage(LogLevel.Warning, "Missing target event store in subscriber context for observer '{ObserverKey}'")]
    internal static partial void MissingTargetEventStore(this ILogger<EventStoreSubscriptionObserverSubscriber> logger, ObserverKey observerKey);

    [LoggerMessage(LogLevel.Debug, "Successfully forwarded events from observer '{ObserverKey}' to event store '{TargetEventStore}' inbox sequence '{InboxSequenceId}'")]
    internal static partial void SuccessfullyForwardedEvents(this ILogger<EventStoreSubscriptionObserverSubscriber> logger, ObserverKey observerKey, EventStoreName targetEventStore, EventSequenceId inboxSequenceId);

    [LoggerMessage(LogLevel.Error, "Error forwarding events from observer '{ObserverKey}' to event store '{TargetEventStore}' inbox sequence '{InboxSequenceId}'")]
    internal static partial void ErrorForwardingEvents(this ILogger<EventStoreSubscriptionObserverSubscriber> logger, Exception exception, ObserverKey observerKey, EventStoreName targetEventStore, EventSequenceId inboxSequenceId);
}
