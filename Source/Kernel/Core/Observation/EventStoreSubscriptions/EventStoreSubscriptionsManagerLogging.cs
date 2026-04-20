// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Observation.EventStoreSubscriptions;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Observation.EventStoreSubscriptions;

/// <summary>
/// Holds log messages for <see cref="EventStoreSubscriptionsManager"/>.
/// </summary>
internal static partial class EventStoreSubscriptionsManagerLogging
{
    [LoggerMessage(LogLevel.Information, "Subscribing event store subscription '{SubscriptionId}' in namespace '{Namespace}'")]
    internal static partial void Subscribing(this ILogger<EventStoreSubscriptionsManager> logger, EventStoreSubscriptionId subscriptionId, EventStoreNamespaceName @namespace);

    [LoggerMessage(LogLevel.Debug, "Event store subscription '{SubscriptionId}' in namespace '{Namespace}' was already subscribed")]
    internal static partial void AlreadySubscribed(this ILogger<EventStoreSubscriptionsManager> logger, EventStoreSubscriptionId subscriptionId, EventStoreNamespaceName @namespace);

    [LoggerMessage(LogLevel.Information, "Unregistering event store subscription '{SubscriptionId}'")]
    internal static partial void Unregistering(this ILogger<EventStoreSubscriptionsManager> logger, EventStoreSubscriptionId subscriptionId);

    [LoggerMessage(LogLevel.Information, "Unsubscribing event store subscription '{SubscriptionId}' in namespace '{Namespace}'")]
    internal static partial void Unsubscribing(this ILogger<EventStoreSubscriptionsManager> logger, EventStoreSubscriptionId subscriptionId, EventStoreNamespaceName @namespace);

    [LoggerMessage(LogLevel.Debug, "Event store subscription '{SubscriptionId}' in namespace '{Namespace}' was already unsubscribed")]
    internal static partial void AlreadyUnsubscribed(this ILogger<EventStoreSubscriptionsManager> logger, EventStoreSubscriptionId subscriptionId, EventStoreNamespaceName @namespace);

    [LoggerMessage(LogLevel.Debug, "Event store subscription '{SubscriptionId}' in namespace '{Namespace}' is already active - skipping unnecessary re-subscription")]
    internal static partial void SubscriptionAlreadyActive(this ILogger<EventStoreSubscriptionsManager> logger, EventStoreSubscriptionId subscriptionId, EventStoreNamespaceName @namespace);

    [LoggerMessage(LogLevel.Error, "Error refreshing event store subscription '{SubscriptionId}' in namespace '{Namespace}'")]
    internal static partial void ErrorRefreshingSubscription(this ILogger<EventStoreSubscriptionsManager> logger, Exception exception, EventStoreSubscriptionId subscriptionId, EventStoreNamespaceName @namespace);

    [LoggerMessage(LogLevel.Information, "Event store subscription '{SubscriptionId}' is ready to receive events")]
    internal static partial void SubscriptionReadyForUse(this ILogger<EventStoreSubscriptionsManager> logger, EventStoreSubscriptionId subscriptionId);

    [LoggerMessage(LogLevel.Warning, "Event store subscription '{SubscriptionId}' was not registered within {Timeout}")]
    internal static partial void SubscriptionDefinitionNotFoundWithinTimeout(this ILogger<EventStoreSubscriptionsManager> logger, EventStoreSubscriptionId subscriptionId, TimeSpan timeout);

    [LoggerMessage(LogLevel.Warning, "Event store subscription '{SubscriptionId}' did not become ready within {Timeout}")]
    internal static partial void SubscriptionNotReadyWithinTimeout(this ILogger<EventStoreSubscriptionsManager> logger, EventStoreSubscriptionId subscriptionId, TimeSpan timeout);

    [LoggerMessage(LogLevel.Information, "Source event store '{SourceEventStore}' became available; retrying {SubscriptionCount} pending subscriptions")]
    internal static partial void SourceEventStoreBecameAvailable(this ILogger<EventStoreSubscriptionsManager> logger, EventStoreName sourceEventStore, int subscriptionCount);

    [LoggerMessage(LogLevel.Error, "Error refreshing subscription '{SubscriptionId}' after source event store '{SourceEventStore}' was added")]
    internal static partial void ErrorRefreshingForNewSourceEventStore(this ILogger<EventStoreSubscriptionsManager> logger, Exception exception, EventStoreSubscriptionId subscriptionId, EventStoreName sourceEventStore);

    [LoggerMessage(LogLevel.Debug, "Event store subscription '{SubscriptionId}' health check failed - refreshing subscription")]
    internal static partial void SubscriptionHealthCheckFailed(this ILogger<EventStoreSubscriptionsManager> logger, EventStoreSubscriptionId subscriptionId);

    [LoggerMessage(LogLevel.Error, "Error processing subscription reminder for subscription")]
    internal static partial void ErrorProcessingSubscriptionReminder(this ILogger<EventStoreSubscriptionsManager> logger, Exception exception, EventStoreSubscriptionId subscriptionId);
}
