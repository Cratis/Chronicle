// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Statistics;

/// <summary>
/// Represents the key figures for an event store, or for one namespace within it.
/// </summary>
/// <param name="TotalEvents">How many events are held in total.</param>
/// <param name="EventTypes">How many distinct event types appear.</param>
/// <param name="Namespaces">How many distinct namespaces hold events.</param>
/// <remarks>
/// Every figure here is a fold over <see cref="EventTypeStatistics"/> rows rather than a stored value of its own -
/// a total that is stored separately from what it totals is a total that can disagree with it.
/// </remarks>
public record EventStoreStatistics(long TotalEvents, int EventTypes, int Namespaces)
{
    /// <summary>
    /// Gets the figures for an event store that holds nothing.
    /// </summary>
    public static readonly EventStoreStatistics Empty = new(0, 0, 0);

    /// <summary>
    /// Folds a set of <see cref="EventTypeStatistics"/> rows into the key figures they describe.
    /// </summary>
    /// <param name="rows">The rows to fold.</param>
    /// <returns>The <see cref="EventStoreStatistics"/>.</returns>
    /// <remarks>
    /// A namespace is only counted when it actually holds events. A namespace that exists but has never been
    /// appended to has no rows, and reporting it as holding zero events would make the namespace count a count of
    /// namespaces created rather than of namespaces in use - which is a different question, asked elsewhere.
    /// </remarks>
    public static EventStoreStatistics From(IEnumerable<EventTypeStatistics> rows)
    {
        var materialized = rows as IReadOnlyCollection<EventTypeStatistics> ?? [.. rows];

        return new(
            materialized.Sum(_ => (long)_.Count),
            materialized.Select(_ => _.EventType).Distinct(StringComparer.Ordinal).Count(),
            materialized.Select(_ => _.Namespace).Distinct(StringComparer.Ordinal).Count());
    }
}
