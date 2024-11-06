// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences;

/// <summary>
/// Extension methods for querying events.
/// </summary>
public static class EventSequenceQueries
{
    static readonly SortDefinition<Event> _sortSequenceNumberDescending = Builders<Event>.Sort.Descending(e => e.SequenceNumber);
    static readonly SortDefinition<Event> _sortSequenceNumberAscending = Builders<Event>.Sort.Ascending(e => e.SequenceNumber);

    /// <summary>
    /// Sort by sequence number.
    /// </summary>
    /// <param name="query">The query to sort.</param>
    /// <returns>The resulting query for continuation.</returns>
    public static IFindFluent<Event, Event> SortByAscendingSequenceNumber(this IFindFluent<Event, Event> query) => query.Sort(_sortSequenceNumberAscending);

    /// <summary>
    /// Sort by sequence number descending.
    /// </summary>
    /// <param name="query">The query to sort.</param>
    /// <returns>The resulting query for continuation.</returns>
    public static IFindFluent<Event, Event> SortByDescendingSequenceNumber(this IFindFluent<Event, Event> query) => query.Sort(_sortSequenceNumberDescending);
}
