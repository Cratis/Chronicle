// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventStoreSubscriptions;

/// <summary>
/// Defines a builder for configuring an event store subscription.
/// </summary>
public interface IEventStoreSubscriptionBuilder
{
    /// <summary>
    /// Specify the event types to subscribe to. If not specified, all events are subscribed.
    /// </summary>
    /// <typeparam name="TEvent">The event type to include.</typeparam>
    /// <returns>The builder for continuation.</returns>
    IEventStoreSubscriptionBuilder WithEventType<TEvent>();

    /// <summary>
    /// Build the subscription definition.
    /// </summary>
    /// <returns>The <see cref="EventStoreSubscriptionDefinition"/>.</returns>
    EventStoreSubscriptionDefinition Build();
}
