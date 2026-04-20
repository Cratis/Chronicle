// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.EventStoreSubscriptions;
using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Observation.EventStoreSubscriptions;

/// <summary>
/// Defines a system that is responsible for supervising event store subscriptions.
/// </summary>
public interface IEventStoreSubscriptionsManager : IGrainWithStringKey
{
    /// <summary>
    /// Ensure the existence of the subscriptions manager.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    Task Ensure();

    /// <summary>
    /// Get all <see cref="EventStoreSubscriptionDefinition">subscription definitions</see> available.
    /// </summary>
    /// <returns>A collection of <see cref="EventStoreSubscriptionDefinition"/>.</returns>
    Task<IEnumerable<EventStoreSubscriptionDefinition>> GetSubscriptionDefinitions();

    /// <summary>
    /// Add an <see cref="EventStoreSubscriptionDefinition"/> for the event store it belongs to.
    /// </summary>
    /// <param name="definition">The <see cref="EventStoreSubscriptionDefinition"/> to add.</param>
    /// <returns>Awaitable task.</returns>
    Task Add(EventStoreSubscriptionDefinition definition);

    /// <summary>
    /// Remove an event store subscription by its identifier.
    /// </summary>
    /// <param name="subscriptionId">The <see cref="EventStoreSubscriptionId"/> to remove.</param>
    /// <returns>Awaitable task.</returns>
    Task Remove(EventStoreSubscriptionId subscriptionId);

    /// <summary>
    /// Wait until a subscription is ready to receive events.
    /// </summary>
    /// <param name="subscriptionId">The <see cref="EventStoreSubscriptionId"/> to wait for.</param>
    /// <param name="timeout">The maximum time to wait.</param>
    /// <returns>Awaitable task.</returns>
    /// <exception cref="TimeoutException">Thrown if the subscription does not become ready within the timeout.</exception>
    Task WaitUntilSubscribed(EventStoreSubscriptionId subscriptionId, TimeSpan timeout);

    /// <summary>
    /// Notifies the manager that a source event store has been added.
    /// Any subscriptions waiting for this source event store should be retried immediately.
    /// </summary>
    /// <param name="sourceEventStore">The source <see cref="EventStoreName"/> that became available.</param>
    /// <returns>Awaitable task.</returns>
    Task SourceEventStoreAdded(EventStoreName sourceEventStore);
}
