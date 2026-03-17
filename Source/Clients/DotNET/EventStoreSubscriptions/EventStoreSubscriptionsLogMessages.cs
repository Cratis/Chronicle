// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.EventStoreSubscriptions;

/// <summary>
/// Holds log messages for <see cref="EventStoreSubscriptions"/>.
/// </summary>
internal static partial class EventStoreSubscriptionsLogMessages
{
    [LoggerMessage(LogLevel.Information, "Subscribing to source event store '{SourceEventStore}' with subscription id '{SubscriptionId}'")]
    internal static partial void Subscribing(this ILogger<EventStoreSubscriptions> logger, EventStoreSubscriptionId subscriptionId, string sourceEventStore);

    [LoggerMessage(LogLevel.Information, "Unsubscribing from subscription '{SubscriptionId}'")]
    internal static partial void Unsubscribing(this ILogger<EventStoreSubscriptions> logger, EventStoreSubscriptionId subscriptionId);
}
