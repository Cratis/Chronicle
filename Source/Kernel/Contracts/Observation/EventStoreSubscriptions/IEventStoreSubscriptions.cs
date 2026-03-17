// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Observation.EventStoreSubscriptions;

/// <summary>
/// Defines the contract for working with event store subscriptions.
/// </summary>
[Service]
public interface IEventStoreSubscriptions
{
    /// <summary>
    /// Add event store subscriptions.
    /// </summary>
    /// <param name="request">The <see cref="AddEventStoreSubscriptions"/> holding the subscriptions to add.</param>
    /// <param name="context">gRPC call context.</param>
    /// <returns>Awaitable task.</returns>
    [Operation]
    Task Add(AddEventStoreSubscriptions request, CallContext context = default);

    /// <summary>
    /// Remove event store subscriptions.
    /// </summary>
    /// <param name="request">The <see cref="RemoveEventStoreSubscriptions"/> request.</param>
    /// <param name="context">gRPC call context.</param>
    /// <returns>Awaitable task.</returns>
    [Operation]
    Task Remove(RemoveEventStoreSubscriptions request, CallContext context = default);

    /// <summary>
    /// Gets all event store subscriptions for a target event store.
    /// </summary>
    /// <param name="request"><see cref="GetEventStoreSubscriptionsRequest"/>.</param>
    /// <returns><see cref="IEnumerable{T}"/> of <see cref="EventStoreSubscriptionDefinition"/>.</returns>
    [Operation]
    Task<IEnumerable<EventStoreSubscriptionDefinition>> GetSubscriptions(GetEventStoreSubscriptionsRequest request);
}
