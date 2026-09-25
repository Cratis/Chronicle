// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Statistics;

/// <summary>
/// Converter methods for <see cref="EventStatistics"/>.
/// </summary>
/// <remarks>
/// Deliberately not a static method on the read model. The proxy generator emits a query for any static method on
/// a read model whose return shape is a supported one, and does not look at accessibility - so a fold helper left
/// there becomes a public rpc taking a list of rows, which is both meaningless as an endpoint and a way to make
/// the kernel compute statistics from whatever a caller cares to send it.
/// </remarks>
internal static class EventStatisticsConverters
{
    /// <summary>
    /// Folds the rows into the shape the client reads.
    /// </summary>
    /// <param name="rows">The rows to fold.</param>
    /// <returns>The <see cref="EventStatistics"/>.</returns>
    /// <remarks>
    /// The breakdown collapses the namespace out, because a caller asking an event store wide question wants one
    /// row per event type rather than one per pair - and a caller asking about a single namespace already has only
    /// that namespace's rows, so the same fold answers both.
    /// </remarks>
    internal static EventStatistics ToEventStatistics(this IEnumerable<EventTypeStatistics> rows)
    {
        var materialized = rows as IReadOnlyCollection<EventTypeStatistics> ?? [.. rows];
        var figures = EventStoreStatistics.From(materialized);

        var perEventType = materialized
            .GroupBy(row => row.EventType, StringComparer.Ordinal)
            .Select(group => new EventTypeCount(group.Key, group.Sum(row => (long)row.Count)))
            .OrderByDescending(_ => _.Count)
            .ThenBy(_ => _.EventType, StringComparer.Ordinal)
            .ToArray();

        return new(figures.TotalEvents, figures.EventTypes, figures.Namespaces, perEventType);
    }
}
