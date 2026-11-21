// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Concepts.Observation.EventStoreSubscriptions;

/// <summary>
/// Represents the definition of an event store subscription.
/// </summary>
/// <param name="Identifier">The unique <see cref="EventStoreSubscriptionId"/> for the subscription.</param>
/// <param name="SourceEventStore">The source <see cref="EventStoreName"/> whose outbox to subscribe to.</param>
/// <param name="EventTypes">The event types to subscribe to.</param>
public record EventStoreSubscriptionDefinition(
    EventStoreSubscriptionId Identifier,
    EventStoreName SourceEventStore,
    IEnumerable<EventType> EventTypes)
{
    /// <summary>
    /// Gets the policy for delivering event generations.
    /// </summary>
    public EventGenerationDelivery GenerationDelivery { get; init; } = EventGenerationDelivery.Compatibility;

    /// <summary>
    /// Gets the named-tag filter. Several values of one tag name are alternatives, different names must all match,
    /// and untagged events never match a non-empty filter. An empty filter matches every event.
    /// </summary>
    public IEnumerable<NamedTag> NamedTags { get; init; } = [];

    /// <summary>
    /// Gets the source namespaces the subscription observes. Empty means every namespace. Names are compared case-insensitively
    /// and a namespace outside a non-empty list is neither observed nor created in the source event store.
    /// </summary>
    public IEnumerable<EventStoreNamespaceName> SourceNamespaces { get; init; } = [];
}
