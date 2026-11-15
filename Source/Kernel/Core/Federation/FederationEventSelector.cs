// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation.EventStoreSubscriptions;

namespace Cratis.Chronicle.Federation;

/// <summary>
/// Decides which events to request and which of the pulled events to forward, from the subscriptions of a binding.
/// </summary>
public class FederationEventSelector
{
    readonly IReadOnlyList<FederationSubscription> _subscriptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="FederationEventSelector"/> class.
    /// </summary>
    /// <param name="subscriptions">The subscriptions of the binding.</param>
    public FederationEventSelector(IEnumerable<FederationSubscription> subscriptions)
    {
        _subscriptions = [.. subscriptions];
        EventTypeIds = [.. _subscriptions.SelectMany(_ => _.EventTypeIds).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal)];
    }

    /// <summary>
    /// Gets the union of the event type ids of all subscriptions: what to request from the producer.
    /// </summary>
    public IReadOnlyList<string> EventTypeIds { get; }

    /// <summary>
    /// Decide whether a pulled event is wanted by at least one subscription.
    /// </summary>
    /// <param name="eventType">The event type, as an id or as id plus generation.</param>
    /// <returns>True when the event is wanted.</returns>
    /// <remarks>
    /// The federation wire carries plain tags only, never named tags, so an event has no named tags here. Like everywhere else in
    /// Chronicle, an event without named tags never satisfies a non-empty filter: a subscription that filters on named tags
    /// forwards nothing over federation rather than leaking events it was meant to hold back.
    /// </remarks>
    public bool Wants(string eventType)
    {
        var id = eventType.Split('+')[0];
        return _subscriptions.Any(_ =>
            _.EventTypeIds.Contains(id, StringComparer.Ordinal) &&
            EventStoreSubscriptionTagFilter.Matches(_.NamedTags, []));
    }
}
