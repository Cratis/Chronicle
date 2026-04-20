// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Observation.EventStoreSubscriptions;

/// <summary>
/// Holds log messages for <see cref="EventStoreSubscriptionsReactor"/>.
/// </summary>
internal static partial class EventStoreSubscriptionsReactorLogging
{
    [LoggerMessage(LogLevel.Information, "Adding event store subscription for event source '{EventSourceId}'")]
    internal static partial void AddingSubscription(this ILogger<EventStoreSubscriptionsReactor> logger, EventSourceId eventSourceId);

    [LoggerMessage(LogLevel.Information, "Event store subscription '{SubscriptionId}' added for event source '{EventSourceId}'")]
    internal static partial void SubscriptionAdded(this ILogger<EventStoreSubscriptionsReactor> logger, EventSourceId eventSourceId, Concepts.Observation.EventStoreSubscriptions.EventStoreSubscriptionId subscriptionId);

    [LoggerMessage(LogLevel.Information, "Removing event store subscription for event source '{EventSourceId}'")]
    internal static partial void RemovingSubscription(this ILogger<EventStoreSubscriptionsReactor> logger, EventSourceId eventSourceId);

    [LoggerMessage(LogLevel.Information, "Event store subscription removed for event source '{EventSourceId}'")]
    internal static partial void SubscriptionRemoved(this ILogger<EventStoreSubscriptionsReactor> logger, EventSourceId eventSourceId);

    [LoggerMessage(LogLevel.Information, "Source event store '{SourceEventStore}' was added; checking pending subscriptions across {TargetEventStoreCount} event stores")]
    internal static partial void SourceEventStoreAdded(this ILogger<EventStoreSubscriptionsReactor> logger, EventStoreName sourceEventStore, int targetEventStoreCount);

    [LoggerMessage(LogLevel.Error, "Error retrying pending subscriptions for target event store '{TargetEventStore}' after source event store '{SourceEventStore}' was added")]
    internal static partial void ErrorRetryingPendingSubscriptions(this ILogger<EventStoreSubscriptionsReactor> logger, Exception exception, EventStoreName targetEventStore, EventStoreName sourceEventStore);
}
