// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventStoreSubscriptions;

/// <summary>
/// Defines the API for managing event store subscriptions.
/// </summary>
public interface IEventStoreSubscriptions
{
    /// <summary>
    /// Subscribe to events from a source event store's outbox.
    /// </summary>
    /// <param name="subscriptionId">The unique identifier for this subscription.</param>
    /// <param name="sourceEventStore">The name of the source event store to subscribe to.</param>
    /// <param name="configure">Optional callback to configure the subscription (e.g. filter event types).</param>
    /// <returns>Awaitable task.</returns>
    Task Subscribe(EventStoreSubscriptionId subscriptionId, string sourceEventStore, Action<IEventStoreSubscriptionBuilder>? configure = default);

    /// <summary>
    /// Remove a subscription by its identifier.
    /// </summary>
    /// <param name="subscriptionId">The <see cref="EventStoreSubscriptionId"/> to remove.</param>
    /// <returns>Awaitable task.</returns>
    Task Unsubscribe(EventStoreSubscriptionId subscriptionId);
}
