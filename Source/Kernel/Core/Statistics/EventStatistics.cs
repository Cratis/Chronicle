// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries.ModelBound;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Grpc;

namespace Cratis.Chronicle.Statistics;

/// <summary>
/// Represents the queryable event statistics for an event store and its namespaces.
/// </summary>
/// <param name="TotalEvents">How many events are held in total.</param>
/// <param name="EventTypes">How many distinct event types appear.</param>
/// <param name="Namespaces">How many distinct namespaces hold events.</param>
/// <param name="PerEventType">The per event type breakdown.</param>
[ReadModel]
[BelongsTo(WellKnownServices.Statistics)]
public record EventStatistics(
    long TotalEvents,
    int EventTypes,
    int Namespaces,
    IEnumerable<EventTypeCount> PerEventType)
{
    /// <summary>
    /// Gets the statistics for an entire event store, across every namespace.
    /// </summary>
    /// <param name="reader">The <see cref="IStatisticsReader"/> to read through.</param>
    /// <param name="eventStore">The event store to get statistics for.</param>
    /// <returns>The <see cref="EventStatistics"/>.</returns>
    internal static async Task<EventStatistics> StatisticsForEventStore(IStatisticsReader reader, EventStoreName eventStore) =>
        (await reader.GetEventTypeStatisticsForEventStore(eventStore)).ToEventStatistics();

    /// <summary>
    /// Gets the statistics for one namespace of an event store.
    /// </summary>
    /// <param name="reader">The <see cref="IStatisticsReader"/> to read through.</param>
    /// <param name="eventStore">The event store to get statistics for.</param>
    /// <param name="namespace">The namespace to get statistics for.</param>
    /// <returns>The <see cref="EventStatistics"/>.</returns>
    internal static async Task<EventStatistics> StatisticsForNamespace(IStatisticsReader reader, EventStoreName eventStore, EventStoreNamespaceName @namespace) =>
        (await reader.GetEventTypeStatistics(eventStore, @namespace)).ToEventStatistics();
}
