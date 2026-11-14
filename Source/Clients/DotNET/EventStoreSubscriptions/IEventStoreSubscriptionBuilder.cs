// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.EventStoreSubscriptions;

/// <summary>
/// Defines a builder for configuring an event store subscription.
/// </summary>
public interface IEventStoreSubscriptionBuilder
{
    /// <summary>
    /// Sets the generation delivery policy. Compatibility is the default.
    /// </summary>
    /// <param name="generationDelivery">The generation delivery policy.</param>
    /// <returns>The builder for continuation.</returns>
    /// <exception cref="EventStoreSubscriptionGenerationDeliveryNotSupported">The builder does not support pinned delivery.</exception>
    IEventStoreSubscriptionBuilder WithGenerationDelivery(EventGenerationDelivery generationDelivery) =>
        generationDelivery == EventGenerationDelivery.Compatibility ? this : throw new EventStoreSubscriptionGenerationDeliveryNotSupported();

    /// <summary>
    /// Only deliver events carrying the named tag. Several values of one name are alternatives (OR);
    /// different names must all match (AND). Untagged events never match a tag filter, and the event
    /// type filter still applies. Without any tag filter every event of the subscribed types is delivered.
    /// </summary>
    /// <param name="name">The tag name.</param>
    /// <param name="value">The tag value.</param>
    /// <returns>The builder for continuation.</returns>
    /// <exception cref="EventStoreSubscriptionNamedTagsNotSupported">The builder does not support named tag filters.</exception>
    IEventStoreSubscriptionBuilder WithNamedTag(string name, string value) =>
        throw new EventStoreSubscriptionNamedTagsNotSupported();

    /// <summary>
    /// Only deliver events addressed to the given audience, that is events tagged with the audience tag naming <paramref name="audience"/>.
    /// </summary>
    /// <param name="audience">The audience value.</param>
    /// <returns>The builder for continuation.</returns>
    /// <exception cref="EventStoreSubscriptionNamedTagsNotSupported">The builder does not support named tag filters.</exception>
    IEventStoreSubscriptionBuilder ForAudience(string audience) => WithNamedTag(WellKnownTags.Audience, audience);

    /// <summary>
    /// Only observe the named namespace of the source event store. Without any, every namespace is observed.
    /// A namespace outside the list is neither observed nor created in the source event store. Names are compared
    /// case-insensitively, and a name containing a comma is refused.
    /// </summary>
    /// <param name="namespace">The source namespace name.</param>
    /// <returns>The builder for continuation.</returns>
    /// <exception cref="EventStoreSubscriptionSourceNamespacesNotSupported">The builder does not support restricting source namespaces.</exception>
    /// <exception cref="ArgumentException">The name is empty, whitespace or contains a comma.</exception>
    IEventStoreSubscriptionBuilder WithSourceNamespace(string @namespace) =>
        throw new EventStoreSubscriptionSourceNamespacesNotSupported();

    /// <summary>
    /// Only observe the named namespaces of the source event store. See <see cref="WithSourceNamespace"/>.
    /// </summary>
    /// <param name="namespaces">The source namespace names.</param>
    /// <returns>The builder for continuation.</returns>
    /// <exception cref="EventStoreSubscriptionSourceNamespacesNotSupported">The builder does not support restricting source namespaces.</exception>
    /// <exception cref="ArgumentException">A name is empty, whitespace or contains a comma.</exception>
    IEventStoreSubscriptionBuilder WithSourceNamespaces(params string[] namespaces)
    {
        var builder = this;
        foreach (var @namespace in namespaces)
        {
            builder = builder.WithSourceNamespace(@namespace);
        }

        return builder;
    }

    /// <summary>
    /// Specify the event types to subscribe to. If not specified, all events are subscribed.
    /// </summary>
    /// <typeparam name="TEvent">The event type to include.</typeparam>
    /// <returns>The builder for continuation.</returns>
    IEventStoreSubscriptionBuilder WithEventType<TEvent>();

    /// <summary>
    /// Specify an event type by its identifier to subscribe to.
    /// </summary>
    /// <param name="eventTypeId">The <see cref="EventTypeId"/> of the event type to include.</param>
    /// <returns>The builder for continuation.</returns>
    IEventStoreSubscriptionBuilder WithEventType(EventTypeId eventTypeId);

    /// <summary>
    /// Specifies an exact event generation by its identifier.
    /// </summary>
    /// <param name="eventTypeId">The event type identifier.</param>
    /// <param name="generation">The declared event generation.</param>
    /// <returns>The builder for continuation.</returns>
    /// <exception cref="EventStoreSubscriptionGenerationDeliveryNotSupported">The builder does not support exact generation declarations.</exception>
    IEventStoreSubscriptionBuilder WithEventType(EventTypeId eventTypeId, EventTypeGeneration generation) =>
        throw new EventStoreSubscriptionGenerationDeliveryNotSupported();

    /// <summary>
    /// Build the subscription definition.
    /// </summary>
    /// <returns>The <see cref="EventStoreSubscriptionDefinition"/>.</returns>
    EventStoreSubscriptionDefinition Build();
}
