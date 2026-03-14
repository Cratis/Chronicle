// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.EventStoreSubscriptions;

namespace Cratis.Chronicle.Observation.EventStoreSubscriptions;

/// <summary>
/// Represents the state of the <see cref="EventStoreSubscriptionsManager"/> grain.
/// </summary>
public class EventStoreSubscriptionsState
{
    /// <summary>
    /// Gets or sets the collection of <see cref="EventStoreSubscriptionDefinition"/> instances.
    /// </summary>
    public IEnumerable<EventStoreSubscriptionDefinition> Subscriptions { get; set; } = [];
}
