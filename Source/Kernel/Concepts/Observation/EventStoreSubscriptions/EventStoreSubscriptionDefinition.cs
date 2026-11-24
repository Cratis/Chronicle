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
[GenerateSerializer]
[Alias("Cratis.Chronicle.Concepts.Observation.EventStoreSubscriptions.EventStoreSubscriptionDefinition")]
public record EventStoreSubscriptionDefinition(
    [Id(0)] EventStoreSubscriptionId Identifier,
    [Id(1)] EventStoreName SourceEventStore,
    [Id(2)] IEnumerable<EventType> EventTypes)
{
    /// <summary>
    /// Gets the policy for delivering event generations.
    /// </summary>
    [Id(3)]
    public EventGenerationDelivery GenerationDelivery { get; init; } = EventGenerationDelivery.Compatibility;

    /// <summary>
    /// Gets the named-tag filter. Several values of one tag name are alternatives, different names must all match,
    /// and untagged events never match a non-empty filter. An empty filter matches every event.
    /// </summary>
    [Id(4)]
    public IEnumerable<NamedTag> NamedTags { get; init; } = [];

    /// <summary>
    /// Gets the retired source namespace restriction. Nothing reads it, nothing sets it and it is always empty: a subscription
    /// observes every namespace its source event store has, and never creates one.
    /// </summary>
    /// <remarks>
    /// Only here because this definition is a grain call payload, and a payload member cannot be removed within a major release
    /// without silos of the previous release failing to read it. Remove it in the next major release.
    /// </remarks>
    [Id(5)]
    [Obsolete("Retired. A subscription observes every namespace its source event store has. Kept only for wire compatibility within the major release.")]
    public IEnumerable<EventStoreNamespaceName> SourceNamespaces { get; init; } = [];
}
