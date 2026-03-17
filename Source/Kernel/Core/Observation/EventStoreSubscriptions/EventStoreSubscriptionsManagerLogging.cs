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
}
