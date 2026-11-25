// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Storage.Observation;

/// <summary>
/// The event filters of a durable subscription, stored as plain values so every storage provider can persist them.
/// </summary>
/// <param name="Tags">The tags an event has to carry, or none to accept every event.</param>
/// <param name="EventSourceType">The event source type to accept, or an empty string for any.</param>
/// <param name="EventStreamType">The event stream type to accept, or an empty string for any.</param>
[GenerateSerializer]
[Alias("Cratis.Chronicle.Storage.Observation.ObserverDurableSubscriptionFilters")]
public record ObserverDurableSubscriptionFilters([Id(0)] IReadOnlyList<string> Tags, [Id(1)] string EventSourceType, [Id(2)] string EventStreamType)
{
    /// <summary>
    /// Creates the stored filters of an <see cref="ObserverFilters"/>.
    /// </summary>
    /// <param name="filters">The filters of the live subscription, if any.</param>
    /// <returns>The stored filters, or null when the subscription has none.</returns>
    public static ObserverDurableSubscriptionFilters? From(ObserverFilters? filters) =>
        filters is null
            ? null
            : new(
                [.. filters.Tags],
                filters.EventSourceType?.Value ?? string.Empty,
                filters.EventStreamType?.Value ?? string.Empty);

    /// <summary>
    /// Restores the <see cref="ObserverFilters"/> of the live subscription.
    /// </summary>
    /// <returns>The restored filters.</returns>
    public ObserverFilters ToObserverFilters() =>
        new(
            Tags,
            string.IsNullOrEmpty(EventSourceType) ? null : new EventSourceType(EventSourceType),
            string.IsNullOrEmpty(EventStreamType) ? null : new EventStreamType(EventStreamType));
}
